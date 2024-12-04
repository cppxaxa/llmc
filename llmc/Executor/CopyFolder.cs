using llmc.Connector;
using llmc.Project;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Executor;

internal class CopyFolder : ExecutorCommon
{
    public override string Execute(string parentDirectory, string param)
    {
        StringBuilder undo = new();

        Dictionary<string, string> p = Common.ParseParam(param);
        string source = p["source"];
        string destination = p["destination"];

        if (Directory.Exists(Path.Join(parentDirectory, destination)))
        {
            Directory.Move(
                Path.Join(parentDirectory, destination),
                Path.Join(parentDirectory, destination + ".bak-" + Guid.NewGuid()));
        }

        // If windows use robocopy, otherwise use rsync.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ProcessStartInfo psi = new()
            {
                FileName = "robocopy",
                Arguments = $"\"{Path.Join(parentDirectory, source)}\" \"{Path.Join(parentDirectory, destination)}\" /E /Z /R:5 /W:5",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process proc = Process.Start(psi)!;
            proc.WaitForExit();
        }
        else
        {
            // Untested.
            ProcessStartInfo psi = new()
            {
                FileName = "rsync",
                Arguments = $"-a \"{Path.Join(parentDirectory, source)}\" \"{Path.Join(parentDirectory, destination)}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process proc = Process.Start(psi)!;
            proc.WaitForExit();
        }


        return undo.ToString();
    }
}
