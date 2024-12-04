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
    internal class WriteFile : ExecutorCommon
    {
        public override string Execute(string parentDirectory, string param)
        {
            StringBuilder undo = new();

            Dictionary<string, string> p = Common.ParseParam(param);
            string filename = p["filename"];
            string escapedString = p["escapedString"];

            if (File.Exists(Path.Join(parentDirectory, filename)))
            {
                string newFilename = filename + ".bak-" + Guid.NewGuid();
                File.Move(Path.Join(parentDirectory, filename), Path.Join(parentDirectory, newFilename));

                undo.AppendLine($"MoveFile(from=\"{newFilename}\",to=\"{filename}\")");
            }

            File.WriteAllText(Path.Join(parentDirectory, filename), escapedString);

            undo.AppendLine($"DeleteFile(filename=\"{filename}\")");

            return undo.ToString();
        }
    }
}
