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
    public override void Execute(string parentDirectory, string param)
    {
        Console.WriteLine("Executing feature Rewrite100: " + param);

        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));
        EnsureThat.EnsureArg.IsNotNull(Prompt, nameof(Prompt));
        EnsureThat.EnsureArg.IsNotNull(ExecutorFinder, nameof(ExecutorFinder));
        EnsureThat.EnsureArg.IsNotNull(ExecutorInvoker, nameof(ExecutorInvoker));

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
            if (IsFileReadingRequired(fileNames[i], metaFileContents[i]))
            {
                Console.WriteLine($"{nameof(Rewrite100)}:Reading file required: {fileNames[i]}");

                string content = File.ReadAllText(Path.Join(parentDirectory, fileNames[i]));

                contentPrompt.AppendLine($"Filename: {fileNames[i]}{Environment.NewLine}");
                contentPrompt.AppendLine($"Original file content before modification:{Environment.NewLine}{content}{Environment.NewLine}");
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

        for (int i = 0; i < fileNames.Count ; i++)
        {
            if (IsFileRewriteRequired(fileNames[i], metaFileContents[i]))
            {
                Console.WriteLine($"{nameof(Rewrite100)}:Rewriting file required: {fileNames[i]}");

                string prompt = $"{fileModificationPrompt}{Environment.NewLine}" +
                    $"Request Id: {Guid.NewGuid()}{Environment.NewLine}" +
                    $"Actual ask to AI Assistant: Give the full raw content for " +
                    $"file {fileNames[i]} after modification inside a " +
                    $"github code annotation:{Environment.NewLine}";
                string newContent = Connector.Complete(prompt);

                //string rawFileContent = GetRawFileContent(fileNames[i], newContent);
                string rawFileContent = RemoveCodeAnnotation(newContent);

                // Write the file.
                File.WriteAllText(Path.Join(parentDirectory, fileNames[i]), rawFileContent);
            }
        }
    }

    private string GetRawFileContent(string filename, string contentToParse)
    {
        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));

        string html1 =
            $"<html>{Environment.NewLine}" +
            $"  <head>Hello</head>{Environment.NewLine}" +
            $"  <body>Hello Everyone!<br/>{Environment.NewLine}" +
            $"  <textarea>{Environment.NewLine}" +
            $"  ```markdown{Environment.NewLine}" +
            $"  ## Header{Environment.NewLine}" +
            $"  ```{Environment.NewLine}" +
            $"  </textarea></body>{Environment.NewLine}" +
            $"</html>{Environment.NewLine}";

        string queryPrefix = "Here is the content for the file '{filename}'";

        string query1 = queryPrefix.Replace("{filename}", "abc/greet.html") + $"{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"```html{Environment.NewLine}" +
            html1 +
            $"```{Environment.NewLine}" +
            $"This concludes the html file contents that can help you greet with minimal changes.";

        string promptPrefix = $"# Give me the raw file contents by extracting from a natural langauage " +
            $"converation:{Environment.NewLine}" +
            $"{Environment.NewLine}";

        string prompt = promptPrefix +
            $"# A sample conversation is as follows:{Environment.NewLine}" +
            $"{query1}{Environment.NewLine}" +
            $"---{Environment.NewLine}" +
            $"# Then we extract the raw content of top level annotation, in the case of our " +
            $"sample it is{Environment.NewLine}" +
            $"{html1}{Environment.NewLine}" +
            $"---{Environment.NewLine}" +
            $"Request Id: {Guid.NewGuid()}{Environment.NewLine}" +
            $"# Actual ask for AI Assistant: Now, for the following actual conversation, respond back with the raw content of top level annotations only for file {filename}:{Environment.NewLine}" +
            $"{Environment.NewLine}" +
            $"{contentToParse}{Environment.NewLine}" +
            $"---{Environment.NewLine}";

        string result = Connector.Complete(prompt);

        string resultWithoutCodeAnnotation = RemoveCodeAnnotation(result);

        return resultWithoutCodeAnnotation;
    }

    private static string RemoveCodeAnnotation(string content)
    {
        string separator = Common.FindLineSeparator(content);

        string[] lines = content.Trim().Split(separator, StringSplitOptions.None);

        List<string> lineList = new(lines);

        if (lineList.Count > 0 && lineList[0].StartsWith("```"))
        {
            lineList.RemoveAt(0);
        }

        if (lineList.Count > 0 && lineList[lineList.Count - 1].StartsWith("```"))
        {
            lineList.RemoveAt(lineList.Count - 1);
        }

        return string.Join(separator, lineList);
    }

    private bool IsFileRewriteRequired(string filename, string meta)
    {
        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));

        string prompt = $"Request Id: {Guid.NewGuid()}{Environment.NewLine}" +
            $"Return single word true or false. Based on the user ask on a file, " +
            $"do you think that we should make changes to the file '{filename}' for user query?{Environment.NewLine}" +
            $"Ask on the file:{meta}{Environment.NewLine}" +
            $"AI answer: ";

        string result = Connector.Complete(prompt);

        return result.Contains("true", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsFileReadingRequired(string filename, string meta)
    {
        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));

        string prompt = $"Request Id: {Guid.NewGuid()}{Environment.NewLine}" +
            $"Return single word true or false. Based on the user ask on a file, " +
            $"do you think that we should read the file '{filename}' for reference while doing any operation?{Environment.NewLine}" +
            $"Ask on the file:{meta}{Environment.NewLine}" +
            $"AI answer: ";

        string result = Connector.Complete(prompt);

        return result.Contains("true", StringComparison.OrdinalIgnoreCase);
    }

    private string GetMetaFileContent(string file, string content)
    {
        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));
        EnsureThat.EnsureArg.IsNotNull(Prompt, nameof(Prompt));

        string prompt = $"# Given,{Environment.NewLine}" +
            $"User prompt: {Prompt.Text}{Environment.NewLine}" +
            $"--------{Environment.NewLine}" +
            $"Filename: {file}{Environment.NewLine}" +
            $"Content:{Environment.NewLine}{Environment.NewLine}" +
            $"{content}{Environment.NewLine}" +
            $"--------{Environment.NewLine}" +
            $"Request Id: {Guid.NewGuid()}{Environment.NewLine}" +
            $"Actual ask to AI assistant: Based on the user prompt, " +
            $"Check if the file content is relevant or not. If relevant, " +
            $"Tell me any anything from file content that may be useful to answer user prompt/ " +
            $"or tell if you suspect modifications required in the file for " +
            $"serving our user prompt. " +
            $"You must answer as concisely as possible, " +
            $"but completely understandable.{Environment.NewLine}";

        string result = Connector.Complete(prompt);

        return result;
    }
}
