using llmc.Connector;
using llmc.Executor;
using llmc.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Project;

internal class ProjectLogic(
    string parentPath,
    LlmConnector connector,
    PromptDecorator promptDecorator,
    PromptExtractor promptExtractor,
    ExecutorFinder executorFinder,
    ExecutorInvoker executorInvoker,
    FileRedactor fileRedactor)
{
    public List<string> ReadPrompts()
    {
        List<string> prompts = [];

        foreach (var file in Directory.GetFiles(parentPath))
        {
            if (file.EndsWith(".prompt.txt"))
            {
                string fileContent = File.ReadAllText(file);
                prompts.Add(fileContent);
            }
        }

        if (prompts.Count == 0)
        {
            Console.WriteLine("No prompts found in the project directory");
        }

        return prompts;
    }

    public List<LlmResult> GetLlmResults(List<string> prompts)
    {
        List<LlmResult> results = [];

        foreach (var rawPrompt in prompts)
        {
            Prompt prompt = promptExtractor.Extract(rawPrompt);
            string promptString = promptDecorator.Decorate(prompt.Text);
            PreProcess(prompt, promptString);
            
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
                undo.Insert(0, executorInvoker.Invoke(parentPath, preBuild));
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
            undo.Insert(0, executorInvoker.Invoke(parentPath, finderResult));
        }

        // Post build.
        if (result.Prompt.PostBuild != null)
        {
            foreach (var postBuild in result.Prompt.PostBuild)
            {
                undo.Insert(0, executorInvoker.Invoke(parentPath, postBuild));
            }
        }

        // Append to cleanup.
        foreach (var cleanup in result.Prompt.Metadata.AppendToCleanup)
        {
            undo.Add(cleanup);
        }

        return string.Join(Environment.NewLine, undo);
    }

    public bool InvokeFeature(
        string parentPath, Prompt prompt, ExecutorFinderResult finderResult)
    {
        IFeature? feature = Assembly.GetExecutingAssembly()
            .CreateInstance($"llmc.Features.{finderResult.ClassName}") as IFeature;

        if (feature == null)
        {
            Console.WriteLine($"Feature {finderResult.ClassName} not found");
            return false;
        }
        else
        {
            // Inject dependencies.
            feature.Connector = connector;
            feature.Prompt = prompt;
            feature.ExecutorFinder = executorFinder;
            feature.ExecutorInvoker = executorInvoker;
            feature.FileRedactor = fileRedactor;

            Console.WriteLine($"Executing {finderResult.ClassName} with param {finderResult.Param}");
            feature.Execute(parentPath, finderResult.Param);

            return true;
        }
    }

    internal object? ReadProjectJson()
    {
        var projects = Directory.GetFiles(
            Directory.GetCurrentDirectory(), "*.llmc.json");

        if (projects.Length == 1)
        {
            return projects.First();
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

    internal FeatureResult ProcessFeatures(List<Prompt> prompts)
    {
        bool anyFeatureExecuted = false;

        // Support features.
        foreach (var prompt in prompts)
        {
            if (prompt.Features != null)
            {
                foreach (var feature in prompt.Features)
                {
                    anyFeatureExecuted |= InvokeFeature(parentPath, prompt, feature);
                }
            }
        }

        return new FeatureResult(AnyFeatureProcessed: anyFeatureExecuted);
    }
}
