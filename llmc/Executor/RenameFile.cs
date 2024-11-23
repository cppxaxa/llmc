using llmc.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Executor
{
    internal class RenameFile : IExecutor
    {
        public string Execute(string parentDirectory, string param)
        {
            List<string> undo = [];

            Dictionary<string, string> p = Common.ParseParam(param);

            string? from = p.ContainsKey("from") ? p["from"] : null;
            string? to = p.ContainsKey("to") ? p["to"] : null;

            if (!string.IsNullOrEmpty(from) || !string.IsNullOrEmpty(to))
            {
                // Rename the file from "from" to "to"
                if (System.IO.File.Exists(System.IO.Path.Join(parentDirectory, from)))
                {
                    if (File.Exists(Path.Join(parentDirectory, to)))
                    {
                        string newFilename = to + ".bak-" + Guid.NewGuid();
                        File.Move(Path.Join(parentDirectory, to), Path.Join(parentDirectory, newFilename));

                        undo.Insert(0, $"MoveFile(from=\"{newFilename}\",to=\"{to}\")");
                    }

                    System.IO.File.Move(
                        System.IO.Path.Join(parentDirectory, from),
                        System.IO.Path.Join(parentDirectory, to));

                    undo.Insert(0, $"MoveFile(from=\"{to}\",to=\"{from}\")");
                }
                else
                {
                    Console.WriteLine($"File {from} does not exist.");
                }
            }

            return string.Join(Environment.NewLine, undo);
        }
    }
}
