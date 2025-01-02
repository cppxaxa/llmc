using CSharpReplLib;
using Newtonsoft.Json;
using System.Reflection;

namespace llmc.Repl;

internal class CSharpRepl
{
    public ScriptResult Execute(
        string parentDirectory, string script,
        List<(string, object)> globals,
        List<Assembly> assemblies,
        List<string> usings)
    {
        ScriptHandler scriptHandler = new();

        scriptHandler = scriptHandler
            .AddReferences(typeof(string).Assembly, includeReferencedAssemblies: true)
            .AddReferences(Assembly.GetExecutingAssembly(), includeReferencedAssemblies: true)
            .AddUsings("System", "System.Text", "System.IO", "System.Linq");
        // scriptHandler = scriptHandler.AddGlobals((nameof(Storage) + "1", Storage));

        // Add assemblies.
        foreach (var assembly in assemblies)
        {
            scriptHandler = scriptHandler.AddReferences(
                assembly, includeReferencedAssemblies: true);
        }

        // Add globals.
        foreach (var global in globals)
        {
            scriptHandler = scriptHandler.AddGlobals((global.Item1, global.Item2));
        }

        // Add usings.
        foreach (var usingNamespace in usings)
        {
            scriptHandler = scriptHandler.AddUsings(usingNamespace);
        }

        List<ScriptHandler.ScriptResult> results = [];
        scriptHandler.ScriptResultReceived += (sender, args) =>
        {
            results.Add(args);
        };

        var initSucceeded = scriptHandler.InitScript().GetAwaiter().GetResult();
        var codeSucceeded = scriptHandler.ExecuteCode(script).GetAwaiter().GetResult();

        ScriptHandler.ScriptResult? scriptResult = results.FirstOrDefault();

        string scriptResultString = scriptResult?.Result ?? "\"\"";
        scriptResultString = scriptResultString.Length < 2
            ? "\"\""
            : scriptResultString;

        scriptResultString = scriptResultString[0] == '"'
            ? scriptResultString.Substring(1)
            : scriptResultString;

        scriptResultString = scriptResultString[^1] == '"'
            ? scriptResultString.Substring(0, scriptResultString.Length - 1)
            : scriptResultString;

        return new ScriptResult(
            scriptResultString,
            scriptResult?.IsError ?? false,
            scriptResult?.IsCancelled ?? false);
    }
}
