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
        EnsureThat.EnsureArg.IsNotNull(Storage);
        
        StringBuilder undo = new();

        Dictionary<string, string> p = Common.ParseParam(param);
        string source = p["source"];
        string destination = p["destination"];

        if (Storage.Exists(Path.Join(parentDirectory, destination)))
        {
            Storage.Move(
                Path.Join(parentDirectory, destination),
                Path.Join(parentDirectory, destination + ".bak-" + Guid.NewGuid()));
        }

        Storage.CopyDirectory(
            Path.Join(parentDirectory, source),
            Path.Join(parentDirectory, destination));
        
        return undo.ToString();
    }
}
