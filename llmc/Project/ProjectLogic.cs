using llmc.Connector;
using llmc.Executor;
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
    ExecutorFinder executorFinder)
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
            string result = connector.Complete(promptString);
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
                undo.Insert(0, InvokeExecutor(parentPath, preBuild));
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
            undo.Insert(0, InvokeExecutor(parentPath, finderResult));
        }

        // Post build.
        if (result.Prompt.PostBuild != null)
        {
            foreach (var postBuild in result.Prompt.PostBuild)
            {
                undo.Insert(0, InvokeExecutor(parentPath, postBuild));
            }
        }

        return string.Join(Environment.NewLine, undo);
    }

    private static string InvokeExecutor(string parentPath, ExecutorFinderResult finderResult)
    {
        IExecutor? executor = Assembly.GetExecutingAssembly()
            .CreateInstance($"llmc.Executor.{finderResult.ClassName}") as IExecutor;

        if (executor == null)
        {
            Console.WriteLine($"Executor {finderResult.ClassName} not found");
        }
        else
        {
            Console.WriteLine($"Executing {finderResult.ClassName} with param {finderResult.Param}");
            return executor.Execute(parentPath, finderResult.Param);
        }

        return string.Empty;
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

            InvokeExecutor(parentPath, new ExecutorFinderResult(ClassName: className, Param: param));
        }

        File.Delete(Path.Join(projectPath, "cleanup.executor.txt"));
    }

    internal bool CheckForCleanup(string projectPath)
    {
        return File.Exists(Path.Join(projectPath, "cleanup.executor.txt"));
    }
}
