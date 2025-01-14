using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmcConsumer.LlmcWrapper;

internal static class Common
{
    public static string FindLineSeparator(string rawPrompt)
    {
        if (rawPrompt.Contains("\r\n"))
        {
            return "\r\n";
        }
        else if (rawPrompt.Contains("\n"))
        {
            return "\n";
        }
        else if (rawPrompt.Contains("\r"))
        {
            return "\r";
        }
        else
        {
            return Environment.NewLine;
        }
    }

    internal static Dictionary<string, ConsoleWritelineModel> GetConsoleWritelines(string output)
    {
        string lineSeparator = Common.FindLineSeparator(output);
        Dictionary<string, ConsoleWritelineModel> consoleWriteline = [];

        string[] lines = output.Split(lineSeparator);
        int[] closingTag = lines.Select((e, i) => (e, i))
            .Where(e => e.e == "</consolewriteline>")
            .Select(e => e.i).ToArray();
        int n = 1;

        foreach (int i in closingTag)
        {
            int j = i;
            while (!lines[j].StartsWith("<consolewriteline"))
            {
                j--;
            }

            if (j >= i)
            {
                Console.WriteLine("Error: Invalid consolewriteline tag");
                continue;
            }

            string tag = lines[j]
                .Replace("<consolewriteline tag=\"", string.Empty)
                .Replace("\">", string.Empty);

            string content = string.Join(lineSeparator, lines[(j + 1)..i]);

            if (string.IsNullOrEmpty(tag))
            {
                tag = $"line-{n}";
                n++;
            }

            consoleWriteline.Add(tag, new ConsoleWritelineModel(content));
        }

        return consoleWriteline;
    }
}
