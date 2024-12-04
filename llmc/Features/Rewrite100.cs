using llmc.Connector;
using llmc.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Features;

internal class Rewrite100 : FeatureCommon
{
    public override List<List<ExecutorFinderResult>> Execute(
        string parentDirectory, string param)
    {
        Console.WriteLine("Executing feature: " + param);

        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));
        EnsureThat.EnsureArg.IsNotNull(Prompt, nameof(Prompt));
        EnsureThat.EnsureArg.IsNotNull(ExecutorFinder, nameof(ExecutorFinder));

        Dictionary<string, string> p = Common.ParseParam(param);
        string repo = p["repo"];
        string wildcard = p["wildcard"];

        // Enquire about the files.
        List<string> fileNames = [];
        List<string> metaFileContents = [];

        foreach (var file in Directory.EnumerateFiles(
            Path.Combine(parentDirectory, repo), wildcard, SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(file);
            
            fileNames.Add(file.Substring(Path.Combine(parentDirectory).Length));
            metaFileContents.Add(GetMetaFileContent(file, content));
        }

        // Form header prompt with file info.
        StringBuilder header = new();

        for (int i = 0; i < fileNames.Count; i++)
        {
            Console.WriteLine($"{nameof(Rewrite100)}:Reading meta: {fileNames[i]}::{metaFileContents[i]}");

            header.AppendLine($"Filename: {fileNames[i]}{Environment.NewLine}");
            header.AppendLine($"Modification request:{Environment.NewLine}{metaFileContents[i]}{Environment.NewLine}");
            header.AppendLine($"----{Environment.NewLine}");
        }

        // Form file content prompt.
        StringBuilder contentPrompt = new();

        for (int i = 0; i < metaFileContents.Count; i++)
        {
            if (IsFileReadingRequired(metaFileContents[i]))
            {
                Console.WriteLine($"{nameof(Rewrite100)}:Reading file required: {fileNames[i]}");

                string content = File.ReadAllText(Path.Join(parentDirectory, fileNames[i]));

                contentPrompt.AppendLine($"Filename: {fileNames[i]}{Environment.NewLine}");
                contentPrompt.AppendLine($"Content:{Environment.NewLine}{content}{Environment.NewLine}");
                contentPrompt.AppendLine($"----{Environment.NewLine}");
            }
        }

        // Form the final prompt.
        StringBuilder fileModificationPrompt = new();

        fileModificationPrompt.AppendLine($"Reference request: {Prompt.Text}{Environment.NewLine}");
        fileModificationPrompt.AppendLine("----");
        fileModificationPrompt.AppendLine($"Given,{Environment.NewLine}");
        fileModificationPrompt.AppendLine(header.ToString());
        fileModificationPrompt.AppendLine(contentPrompt.ToString());

        List<List<ExecutorFinderResult>> compiledFinderResults = [];

        for (int i = 0; i < fileNames.Count ; i++)
        {
            if (IsFileRewriteRequired(metaFileContents[i]))
            {
                Console.WriteLine($"{nameof(Rewrite100)}:Rewriting file required: {fileNames[i]}");

                string prompt = $"{fileModificationPrompt}{Environment.NewLine}" +
                    $"Give the new content for file {fileNames[i]}:{Environment.NewLine}";
                string newContent = Connector.Complete(prompt);

                string writerContent = $"Write the contents to the file{Environment.NewLine}" +
                    $"Filename: {fileNames[i]}{Environment.NewLine}" +
                    $"Content: {Environment.NewLine}" +
                    $"{newContent}";

                List<ExecutorFinderResult> finderResults = ExecutorFinder.Find(writerContent);

                compiledFinderResults.Add(finderResults);
            }
        }

        return compiledFinderResults;
    }

    private bool IsFileRewriteRequired(string meta)
    {
        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));

        string prompt = $"Return single word true or false. Based on the user ask on a file, " +
            $"do you think that we should make changes to the file for user query?{Environment.NewLine}" +
            $"Ask on the file:{meta}{Environment.NewLine}" +
            $"AI answer: ";

        string result = Connector.Complete(prompt);

        return result.Contains("true", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsFileReadingRequired(string meta)
    {
        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));

        string prompt = $"Return single word true or false. Based on the user ask on a file, " +
            $"do you think that we should read the file for reference while doing any operation?{Environment.NewLine}" +
            $"Ask on the file:{meta}{Environment.NewLine}" +
            $"AI answer: ";

        string result = Connector.Complete(prompt);

        return result.Contains("true", StringComparison.OrdinalIgnoreCase);
    }

    private string GetMetaFileContent(string file, string content)
    {
        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));
        EnsureThat.EnsureArg.IsNotNull(Prompt, nameof(Prompt));

        string prompt = $"# Based on the user prompt, " +
            $"Check if the file content is relevant or not. If relevant, " +
            $"Extract out any content that may be useful to answer user prompt/ " +
            $"of tell if you suspect modifications required in the file for " +
            $"our user prompt from it's content." +
            $"You must answer as concisely as possible, " +
            $"but completely understandable.{Environment.NewLine}" +
            $"# Given,{Environment.NewLine}" +
            $"User prompt: {Prompt.Text}{Environment.NewLine}" +
            $"--------{Environment.NewLine}" +
            $"Filename: {file}{Environment.NewLine}" +
            $"Content:{Environment.NewLine}{Environment.NewLine}" +
            $"{content}";

        string result = Connector.Complete(prompt);

        return result;
    }
}
