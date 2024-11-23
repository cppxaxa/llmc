
using llmc.Connector;
using System.Text.RegularExpressions;

namespace llmc.Project
{
    internal class ExecutorFinder(LlmConnector connector)
    {
        internal List<ExecutorFinderResult> Find(string input)
        {
            string prompt = GetPrompt(input);
            string result = connector.Complete(prompt);
            return ParseResult(result);
        }

        private List<ExecutorFinderResult> ParseResult(string result)
        {
            var separator = Common.FindLineSeparator(result);
            var lines = result.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            List<ExecutorFinderResult> results = [];

            foreach (var line in lines)
            {
                if (line.StartsWith('`'))
                {
                    continue;
                }

                (string className, string param) = ExecutorParser.Parse(line);

                results.Add(new ExecutorFinderResult(ClassName: className, Param: param));
            }

            return results;
        }

        private string GetPrompt(string input)
        {
            return $"You're an AI assistant that can read the content followed after ---{Environment.NewLine}" +
                $"{Environment.NewLine}" +
                $"You have to figure out the function calls and their parameters. Available functions are as follows:{Environment.NewLine}" +
                $"ExecuteCmd(filename=\"\"){Environment.NewLine}" +
                $"ExecuteCmd(batchCode=\"\"){Environment.NewLine}" +
                $"ExecutePwsh(filename=\"\"){Environment.NewLine}" +
                $"ExecutePython(filename=\"\"){Environment.NewLine}" +
                $"ExecuteSh(filename=\"\"){Environment.NewLine}" +
                $"WriteFile(filename=\"\",escapedString=\"\"){Environment.NewLine}" +
                $"RenameFile(from=\"\",to=\"\"){Environment.NewLine}" +
                $"{Environment.NewLine}" +
                $"e.g.{Environment.NewLine}" +
                $"Output:{Environment.NewLine}" +
                $"ExecuteCmd(filename=\"helloworld.bat\"){Environment.NewLine}" +
                $"ExecuteCmd(batchCode=\"echo %DATE% %TIME%\"){Environment.NewLine}" +
                $"{Environment.NewLine}" +
                $"{Environment.NewLine}" +
                $"Hint: If you see a code, and it tells to create a file, then write it to a file first.{Environment.NewLine}" +
                $"Hint: If you see a code, and it tells to simply run it, use the Execute prefixed calls.{Environment.NewLine}" +
                $"---{Environment.NewLine}" +
                $"{input}{Environment.NewLine}" +
                $"{Environment.NewLine}" +
                $"Output:{Environment.NewLine}";
        }
    }
}