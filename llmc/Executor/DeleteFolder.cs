﻿using llmc.Connector;
using llmc.Project;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Executor;

internal class DeleteFolder : ExecutorCommon
{
    public override string Execute(string parentDirectory, string param)
    {
        EnsureThat.EnsureArg.IsNotNull(Storage);
        
        StringBuilder undo = new();

        Dictionary<string, string> p = Common.ParseParam(param);
        string folder = p["folder"];

        if (Storage.Exists(Path.Join(parentDirectory, folder)))
        {
            Storage.DeleteDirectory(Path.Join(parentDirectory, folder), true);
        }

        return undo.ToString();
    }
}
