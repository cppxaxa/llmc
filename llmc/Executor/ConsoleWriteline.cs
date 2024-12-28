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
            string filename = p["filename"];
            string fullPath = Path.Join(parentDirectory, filename);

            Console.WriteLine($"## {nameof(ConsoleWriteline)}");
            Console.WriteLine($"## Filename:{filename}");
            Console.WriteLine($"## FullPath:{fullPath}");
            Console.WriteLine($"## Exists:{Storage.Exists(fullPath)}");

            if (Storage.Exists(fullPath))
            {
                Console.WriteLine($"<content>");
                Console.WriteLine(Storage.ReadAllText(fullPath));
                Console.WriteLine($"</content>");
            }

            return undo.ToString();
        }
    }
}
