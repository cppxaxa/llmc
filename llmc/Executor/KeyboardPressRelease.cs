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

internal class KeyboardPressRelease : ExecutorCommon
{
    public override string Execute(string parentDirectory, string param)
    {
        StringBuilder undo = new();

        Dictionary<string, string> p = Common.ParseParam(param);
        string key = p["key"];

        var keyCode = Enum.Parse<KeyCode>(key);

        var simulator = new EventSimulator();

        simulator.SimulateKeyPress(keyCode);

        simulator.SimulateKeyRelease(keyCode);

        return undo.ToString();
    }
}
