using llmc.Connector;
using llmc.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Executor
{
    internal class ExecuteCmd : ExecutorCommon
    {
        public override string Execute(string parentDirectory, string param)
        {
            Dictionary<string, string> p = Common.ParseParam(param);

            string? filename = p.ContainsKey("filename") ? p["filename"] : null;
            string? batchCode = p.ContainsKey("batchCode") ? p["batchCode"] : null;

            if (!string.IsNullOrEmpty(filename) || !string.IsNullOrEmpty(batchCode))
            {
                // Starting cmd.
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                // startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C " + (filename ?? batchCode);
                startInfo.WorkingDirectory = parentDirectory;
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            }

            return string.Empty;
        }
    }
}
