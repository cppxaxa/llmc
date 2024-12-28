using llmc.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Project;

internal class FileRedactor(
    LlmConnector connector,
    ExecutorInvoker _executorInvoker)
{
    private ExecutorInvoker executorInvoker = _executorInvoker;

    public FileRedactor Clone()
    {
        return new FileRedactor(connector: connector, _executorInvoker: executorInvoker);
    }

    public FileRedactor ChangeExecutorInvoker(ExecutorInvoker _executorInvoker)
    {
        executorInvoker = _executorInvoker;
        return this;
    }

    public void RedactFile(string parentDirectory, string filename, Prompt prompt)
    {
        Console.WriteLine($"FileRedactor:Redacting file {filename}");

        string redactionRequestPrompt = GetPrompt(parentDirectory, filename, prompt);
        string redactionResponse = connector.Complete(redactionRequestPrompt);

        string lines = ParseResponse(redactionResponse);
        executorInvoker.Invoke(
            parentDirectory,
            new ExecutorFinderResult(
                ClassName: "RedactFile",
                Param: $"filename={JsonConvert.SerializeObject(filename)}," +
                $"lines={JsonConvert.SerializeObject(lines)}"));
    }

    public void Undo(string parentDirectory, string filename)
    {
        Console.WriteLine($"FileRedactor:Undoing redaction on file {filename}");

        executorInvoker.Invoke(
            parentDirectory,
            new ExecutorFinderResult(
                ClassName: "UndoRedactedFile",
                Param: $"filename={JsonConvert.SerializeObject(filename)}"));
    }

    private string GetPrompt(string parentDirectory, string filename, Prompt prompt)
    {
        string[] fileContent = File.ReadAllLines(Path.Join(parentDirectory, filename));

        for (int i = 1; i <= fileContent.Length; i++)
        {
            fileContent[i - 1] = $"{i}) {fileContent[i - 1]}";
        }

        string fileContentString = string.Join(Environment.NewLine, fileContent);

        return $"You have to repond back only JSON array of strings in format " +
            $"[\"{{inclusiveStartLineNumber1}}-{{inclusiveEndLineNumber1}}\",\"{{inclusiveStartLineNumber2}}-{{inclusiveEndLineNumber2}}\",] " +
            $"that tells which line range section to redact from file content " +
            $"that may not be required to serve user ask.{Environment.NewLine}" +
            $"Don't be too aggressive, but also try to redact so that it reduces lines of " +
            $"files for further processing." +
            $"---{Environment.NewLine}" +
            $"User ask: Add a unit test by considering README.md file and my python " +
            $"source code that greets everyone.{Environment.NewLine}" +
            $"Filename: repo/prog1.py{Environment.NewLine}" +
            $"File content:{Environment.NewLine}" +
            $"1) import time{Environment.NewLine}2) from lib import log{Environment.NewLine}3){Environment.NewLine}4) def snooze(seconds):{Environment.NewLine}5)   time.sleep(seconds){Environment.NewLine}6)   log.snooze(seconds){Environment.NewLine}7){Environment.NewLine}8) def greet_everyone():{Environment.NewLine}9)   return \"Hello Everyone\"{Environment.NewLine}10)   log.greet_everyone(){Environment.NewLine}11){Environment.NewLine}12) def pp(text):{Environment.NewLine}13)   print(text){Environment.NewLine}---{Environment.NewLine}redaction-json:{Environment.NewLine}```json{Environment.NewLine}[\"4-6\",\"12-13\"]{Environment.NewLine}```{Environment.NewLine}We don't need the `snooze` and `pp` function for writing unit tests for `greet_everyone`{Environment.NewLine}" +
            $"---{Environment.NewLine}" +
            $"User ask: {prompt.Text}{Environment.NewLine}" +
            $"Filename: {filename}{Environment.NewLine}" +
            $"File content:{Environment.NewLine}" +
            $"{fileContentString}{Environment.NewLine}" +
            $"---{Environment.NewLine}" +
            $"redaction-json:{Environment.NewLine}";
    }

    private string ParseResponse(string redactionResponse)
    {
        // Sample input: "```json\n[\"21-75\"]\n```\n"
        string separator = Common.FindLineSeparator(redactionResponse);
        string[] lines = redactionResponse.Split(separator);

        StringBuilder sb = new();

        foreach (string line in lines)
        {
            if (line.StartsWith("```"))
            {
                continue;
            }

            sb.AppendLine(line.Trim());
        }

        return sb.ToString().Trim();
    }
}
