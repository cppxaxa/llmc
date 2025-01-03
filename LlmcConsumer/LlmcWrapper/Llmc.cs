using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmcConsumer.LlmcWrapper;

public class Llmc(LlmcConfiguration configuration)
{
    public LlmcResult Execute(
        bool passthroughStdout, bool printLlmResult,
        string projectCwd, string projectJson,
        Func<string, string, string> funcLlm,
        Func<string, string> funcEmbedding)
    {
        StringBuilder arg = new();

        arg.Append(" --stdinprojectjson");
        arg.Append(" --parentprocessid " + Environment.ProcessId);

        if (configuration.azurellm) arg.Append(" --azurellm");
        if (configuration.azureembedding) arg.Append(" --azureembedding");
        if (configuration.noundo) arg.Append(" --noundo");

        // Start a process to execute the llmc.exe.
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "llmc",
                Arguments = arg.ToString(),
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = projectCwd,
                RedirectStandardInput = true
            }
        };

        if (!process.Start())
        {
            throw new Exception("Failed to start llmc.exe");
        }

        // Write the projectJson to the input stream.
        process.StandardInput.WriteLine(projectJson);

        StringBuilder bufferOutStream = new();
        StringBuilder outStream = new();

        // Read the output from the process.
        while (!process.HasExited)
        {
            char ch = (char)process.StandardOutput.Read();
            bufferOutStream.Append(ch);

            if (passthroughStdout) Console.Write(ch);

            if (ch == '>')
            {
                string text = bufferOutStream.ToString();

                if (text.EndsWith("</llmcprompt>"))
                {
                    string lineSeparator = Common.FindLineSeparator(text);
                    string[] lines = text.Split(lineSeparator);

                    // Find the index of the <llmcprompt>.
                    int promptIndex = Array.IndexOf(lines, "<llmcprompt>");
                    string prompt = string.Join(lineSeparator, lines[(promptIndex + 1)..(lines.Length - 1)]);

                    // Similarly, from prompt, extract <user></user> content.
                    string userPrompt = prompt.Substring(
                        prompt.IndexOf("<user>") + "<user>".Length,
                        prompt.IndexOf("</user>") - prompt.IndexOf("<user>") - "<user>".Length);

                    // Similarly, from prompt, extract <system></system> content.
                    string systemPrompt = prompt.Substring(
                        prompt.IndexOf("<system>") + "<system>".Length,
                        prompt.IndexOf("</system>") - prompt.IndexOf("<system>") - "<system>".Length);

                    StringBuilder response = new();
                    response.Append("<response>");
                    response.Append(funcLlm(systemPrompt, userPrompt));
                    response.Append("</response>");

                    // Write the response to the input stream.
                    process.StandardInput.Write(response.ToString());

                    if (printLlmResult)
                    {
                        var color = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(response.ToString());
                        Console.ForegroundColor = color;
                    }

                    Console.WriteLine($"LlmcConsumer: Responded with LLM response.");

                    outStream.Append(bufferOutStream.ToString());
                    bufferOutStream.Clear();
                }
                else if (text.EndsWith("</llmcembeddingprompt>"))
                {
                    string lineSeparator = Common.FindLineSeparator(text);
                    string[] lines = text.Split(lineSeparator);

                    // Find the index of the <llmcprompt>.
                    int promptIndex = Array.IndexOf(lines, "<llmcembeddingprompt>");
                    string prompt = string.Join(lineSeparator, lines[(promptIndex + 1)..(lines.Length - 1)]);

                    StringBuilder response = new();
                    response.Append("<response>");
                    response.Append(funcEmbedding(prompt));
                    response.Append("</response>");

                    // Write the response to the input stream.
                    process.StandardInput.Write(response.ToString());

                    if (printLlmResult)
                    {
                        var color = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"LlmcConsumer: Responded with embedding response.");
                        Console.ForegroundColor = color;
                    }

                    outStream.Append(bufferOutStream.ToString());
                    bufferOutStream.Clear();
                }
            }
        }

        outStream.Append(bufferOutStream);
        outStream.Append(process.StandardOutput.ReadToEnd());
        var consoleWriteline = Common.GetConsoleWritelines(outStream.ToString());

        return new LlmcResult(ConsoleWriteline: consoleWriteline);
    }
}
