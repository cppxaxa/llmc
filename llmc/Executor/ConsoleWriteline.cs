using llmc.Connector;
using llmc.Project;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Executor
{
    internal class ConsoleWriteline : ExecutorCommon
    {
        public override string Execute(string parentDirectory, string param)
        {
            EnsureThat.EnsureArg.IsNotNull(Storage);

            StringBuilder undo = new();

            Dictionary<string, string> p = Common.ParseParam(param);
            string nlines = p.ContainsKey("nlines") ? p["nlines"] : "-1";
            int n = int.Parse(nlines);
            string filename = p["filename"];
            string fullPath = Path.Join(parentDirectory, filename);

            Console.WriteLine($"<consolewriteline>");
            Console.WriteLine($"## FullPath:{fullPath}");
            Console.WriteLine($"## Exists:{Storage.Exists(fullPath)}");

            if (Storage.Exists(fullPath))
            {
                string content = Storage.ReadAllText(fullPath);

                if (n > 0)
                {
                    string sep = Common.FindLineSeparator(content);
                    string[] lines = content.Split(sep);
                    content = string.Join(sep, lines.Take(n));
                }

                Console.WriteLine(content);
            }

            Console.WriteLine($"</consolewriteline>");

            return undo.ToString();
        }
    }
}
