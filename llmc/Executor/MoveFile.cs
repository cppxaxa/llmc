using llmc.Connector;
using llmc.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Executor
{
    internal class MoveFile : ExecutorCommon
    {
        public override string Execute(string parentDirectory, string param)
        {
            EnsureThat.EnsureArg.IsNotNull(Storage);

            List<string> undo = [];

            Dictionary<string, string> p = Common.ParseParam(param);

            string? from = p.ContainsKey("from") ? p["from"] : null;
            string? to = p.ContainsKey("to") ? p["to"] : null;

            if (!string.IsNullOrEmpty(from) || !string.IsNullOrEmpty(to))
            {
                // Rename the file from "from" to "to"
                if (Storage.Exists(Path.Join(parentDirectory, from)))
                {
                    if (Storage.Exists(Path.Join(parentDirectory, to)))
                    {
                        string newFilename = to + ".bak-" + Guid.NewGuid();
                        Storage.Move(Path.Join(parentDirectory, to), Path.Join(parentDirectory, newFilename));

                        undo.Insert(0, $"MoveFile(from=\"{newFilename}\",to=\"{to}\")");
                    }

                    Storage.Move(
                        Path.Join(parentDirectory, from),
                        Path.Join(parentDirectory, to));

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
