using llmc.Connector;
using llmc.Project;
using Newtonsoft.Json;
using SharpHook;
using SharpHook.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Executor;

internal class MouseMove : ExecutorCommon
{
    public override string Execute(string parentDirectory, string param)
    {
        StringBuilder undo = new();

        Dictionary<string, string> p = Common.ParseParam(param);
        string x = p["x"];
        string y = p["y"];

        var simulator = new EventSimulator();

        simulator.SimulateMouseMovement(short.Parse(x), short.Parse(y));

        return undo.ToString();
    }
}
