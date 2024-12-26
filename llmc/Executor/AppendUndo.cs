using llmc.Project;
using System.Text;

namespace llmc.Executor;

internal class AppendUndo : ExecutorCommon
{
    public override string Execute(string parentDirectory, string param)
    {
        StringBuilder undo = new();

        string undoFilename = Path.Combine(parentDirectory, "undo.executor.txt");

        Dictionary<string, string> p = Common.ParseParam(param);
        string? dump = p.TryGetValue("dump", out string? strVal) ? strVal : null;

        if (dump != null)
        {
            File.AppendAllLines(undoFilename, new[] { dump });
            return undo.ToString();
        }
        else
        {
            string source = p["fn"];

            List<string> remainingParams = p.Where(e => e.Key != "fn")
                .Select(e => $"{e.Key}=\"{e.Value}\"").ToList();

            File.AppendAllLines(undoFilename, new[] {
                $"{source}({string.Join(',', remainingParams)})"
            });
        }

        return undo.ToString();
    }
}
