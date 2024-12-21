using llmc.Project;
using System.Text;

namespace llmc.Executor;

internal class AppendUndo : ExecutorCommon
{
    public override string Execute(string parentDirectory, string param)
    {
        StringBuilder undo = new();

        Dictionary<string, string> p = Common.ParseParam(param);
        string source = p["fn"];
        List<string> remainingParams = p.Where(e => e.Key != "fn")
            .Select(e => $"{e.Key}=\"{e.Value}\"").ToList();

        undo.Append($"{source}({string.Join(',', remainingParams)})");
        
        return undo.ToString();
    }
}
