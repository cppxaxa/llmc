using llmc.Connector;
using llmc.Executor;
using llmc.Features;
using llmc.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Project;

internal class ProjectLogic(
    string parentPath,
    ProjectModel project,
    CommandLineParams commandLineParams,
    IStorage storage,
    LlmConnector connector,
    PromptDecorator promptDecorator,
    PromptExtractor promptExtractor,
    ExecutorFinder executorFinder,
    ExecutorInvoker executorInvoker,
    FileRedactor fileRedactor)
{
    public void Validate()
    {
        if (project == null)
        {
            throw new ArgumentException("Project file not found", nameof(project));
        }

        if (project.Macros.Keys
            .Where(e => !e.StartsWith("{{{") || !e.EndsWith("}}}")).Any())
        {
            throw new ArgumentException(
                "Macro should start with '{{{' and end with '}}}'",
                nameof(project.Macros));
        }
    }

    public List<Prompt> ReadPrompts(bool disableInMemoryStorage)
    {
        List<(string, string)> prompts = [];

        foreach (var file in storage.GetFiles(parentPath))
        {
            if (file.EndsWith(".prompt.txt"))
            {
                string fileContent = File.ReadAllText(file);

                // Apply prompt macros.
                fileContent = Common.ApplyProjectMacros(project, fileContent);

                prompts.Add((Path.GetFileName(file), fileContent));
            }
        }

        if (prompts.Count == 0)
        {
            Console.WriteLine("No prompts found in the project directory");
        }

        return prompts.Select(e => promptExtractor.Extract(e.Item1, e.Item2))
            .Select(e =>
            {
                if (disableInMemoryStorage)
                {
                    e.Flags.Remove(Flag.EnableInMemoryStorage);
                    e.Metadata.Flags.Remove(Flag.EnableInMemoryStorage.ToString());
                    e.StorageConfiguration = new(EnableInMemoryStorage: false);
                }

                return e;
            }).ToList();
    }

    public List<LlmResult> GetLlmResults(List<Prompt> prompts)
    {
        List<LlmResult> results = [];

        foreach (var prompt in prompts)
        {
            string promptString = promptDecorator.Decorate(prompt.Text);
            
            string result = string.Empty;
            if (!string.IsNullOrWhiteSpace(promptString))
            {
                result = connector.Complete(promptString);
            }

            results.Add(new LlmResult(Prompt: prompt, Text: result));
        }

        return results;
    }

    public string Process(List<LlmResult> llmResults)
    {
        List<string> undo = [];

        foreach (var result in llmResults)
        {
            undo.Insert(0, Process(result));
        }

        return string.Join(Environment.NewLine, undo);
    }

    public string PreProcess(Prompt prompt, string promptString)
    {
        Console.WriteLine($"PreProcess individual prompt {prompt}");

        List<string> undo = [];

        // Pre build.
        if (prompt.PreBuild != null)
        {
            foreach (var preBuild in prompt.PreBuild)
            {
                undo.Insert(0, executorInvoker.Clone().ChangeStorage(
                    storage.Clone().ApplyConfiguration(prompt.StorageConfiguration))
                    .Invoke(parentPath, preBuild));
            }
        }

        return string.Join(Environment.NewLine, undo);
    }

    public string PostProcess(Prompt prompt)
    {
        Console.WriteLine($"PreProcess individual prompt {prompt}");

        List<string> undo = [];

        // Pre build.
        if (prompt.PostBuild != null)
        {
            foreach (var postBuild in prompt.PostBuild)
            {
                undo.Insert(0, executorInvoker.Clone().ChangeStorage(
                    storage.Clone().ApplyConfiguration(prompt.StorageConfiguration))
                    .Invoke(parentPath, postBuild));
            }
        }

        return string.Join(Environment.NewLine, undo);
    }

    public string Process(LlmResult result)
    {
        Console.WriteLine($"Process individual result {result}");

        List<ExecutorFinderResult> finderResults = executorFinder.Find(result.Text);

        List<string> undo = [];

        foreach (var finderResult in finderResults)
        {
            undo.Insert(0, executorInvoker.Clone().ChangeStorage(
                storage.Clone().ApplyConfiguration(result.Prompt.StorageConfiguration))
                .Invoke(parentPath, finderResult));
        }

        // Append to cleanup.
        foreach (var cleanup in result.Prompt.Metadata.AppendToCleanup)
        {
            undo.Add(cleanup);
        }

        return string.Join(Environment.NewLine, undo);
    }

    public IFeature? CreateFeature(Prompt prompt, ExecutorFinderResult finderResult)
    {
        IFeature? feature = Assembly.GetExecutingAssembly()
            .CreateInstance($"llmc.Features.{finderResult.ClassName}") as IFeature;

        if (feature == null)
        {
            Console.WriteLine($"Feature {finderResult.ClassName} not found");
            return null;
        }
        else
        {
            // Inject dependencies.
            feature.VerboseLogging = commandLineParams.VerboseLogging;
            feature.NoUndo = commandLineParams.NoUndo;
            feature.Connector = connector;
            feature.Prompt = prompt;
            feature.ExecutorFinder = executorFinder;
            feature.ExecutorInvoker = executorInvoker;
            feature.FileRedactor = fileRedactor;
            feature.Storage = storage.Clone().ApplyConfiguration(prompt.StorageConfiguration);

            return feature;
        }
    }

    public FeatureResult InvokeFeature(
        string parentPath, IFeature feature,
        ExecutorFinderResult finderResult)
    {
        Console.WriteLine($"Executing {finderResult.ClassName} with param {finderResult.Param}");

        FeatureResult featureResult = feature.Execute(parentPath, finderResult.Param);

        return featureResult;
    }

    public static ProjectModel? ReadProjectJson(IStorage storage, string directory)
    {
        var projects = storage.GetFiles(directory, "*.llmc.json");

        if (projects.Length == 1)
        {
            return JsonConvert.DeserializeObject<ProjectModel>(File.ReadAllText(projects[0]));
        }

        if (projects.Length > 1)
        {
            Console.WriteLine("More than one project file found, selecting none");
            return null;
        }

        return null;
    }

    internal void Cleanup(string projectPath)
    {
        string cleanupContent = File.ReadAllText(Path.Join(projectPath, "cleanup.executor.txt"));

        string separator = Common.FindLineSeparator(cleanupContent);

        foreach (var line in cleanupContent.Split(separator, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = line.Split("(", StringSplitOptions.RemoveEmptyEntries);
            string className = parts[0];
            string param = parts[1].Trim(')');

            executorInvoker.Invoke(
                parentPath, new ExecutorFinderResult(ClassName: className, Param: param));
        }

        File.Delete(Path.Join(projectPath, "cleanup.executor.txt"));
    }

    internal bool CheckForCleanup(string projectPath)
    {
        return File.Exists(Path.Join(projectPath, "cleanup.executor.txt"));
    }

    internal MultipleFeatureResult ProcessPrebuildFeatures(List<Prompt> prompts)
    {
        bool anyFeatureExecuted = false;
        List<string> fileNames = [];

        // Support features.
        foreach (var prompt in prompts)
        {
            if (prompt.Features != null)
            {
                foreach (var finderResult in prompt.Features)
                {
                    IFeature? feature = CreateFeature(prompt, finderResult);

                    if (feature != null)
                    {
                        // Skip if not prebuild.
                        if (!feature.AsPrebuild)
                        {
                            continue;
                        }
                        
                        var featureResult = InvokeFeature(parentPath, feature, finderResult);

                        anyFeatureExecuted |= featureResult.Executed;

                        if (!string.IsNullOrEmpty(featureResult.GotoPromptsAfter))
                        {
                            fileNames.Add(featureResult.GotoPromptsAfter);
                        }
                    }
                }
            }
        }

        string? gotoPromptsAfter = fileNames.Count > 0 ? fileNames.Min() : null;

        return new MultipleFeatureResult(
            AnyFeatureProcessed: anyFeatureExecuted, GotoPromptsAfter: gotoPromptsAfter);
    }

    internal MultipleFeatureResult ProcessNonPrebuildFeatures(List<Prompt> prompts)
    {
        bool anyFeatureExecuted = false;
        List<string> fileNames = [];

        // Support features.
        foreach (var prompt in prompts)
        {
            if (prompt.Features != null)
            {
                foreach (var finderResult in prompt.Features)
                {
                    IFeature? feature = CreateFeature(prompt, finderResult);

                    if (feature != null)
                    {
                        // Skip if prebuild.
                        if (feature.AsPrebuild)
                        {
                            continue;
                        }

                        var featureResult = InvokeFeature(
                            parentPath, feature, finderResult);

                        anyFeatureExecuted |= featureResult.Executed;

                        if (!string.IsNullOrEmpty(featureResult.GotoPromptsAfter))
                        {
                            fileNames.Add(featureResult.GotoPromptsAfter);
                        }
                    }
                }
            }
        }

        string? gotoPromptsAfter = fileNames.Count > 0 ? fileNames.Min() : null;

        return new MultipleFeatureResult(
            AnyFeatureProcessed: anyFeatureExecuted, GotoPromptsAfter: gotoPromptsAfter);
    }

    internal static ProjectModel ReadProjectJsonFromStdin()
    {
        ProjectModel? projectModel;
        StringBuilder sb = new();
        sb.Append((char)Console.Read());

        while (!Common.TryParseJson(sb.ToString(), out projectModel))
        {
            int ch = Console.Read();
            sb.Append((char)ch);
        }

        return projectModel!;
    }
}
