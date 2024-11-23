using llmc.Project;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Executor
{
    internal class DeleteFile : IExecutor
    {
        public string Execute(string parentDirectory, string param)
        {
            StringBuilder undo = new();

            Dictionary<string, string> p = Common.ParseParam(param);
            string filename = p["filename"];

            if (File.Exists(Path.Join(parentDirectory, filename)))
            {
                File.Delete(Path.Join(parentDirectory, filename));
            }

            return undo.ToString();
        }
    }
}
