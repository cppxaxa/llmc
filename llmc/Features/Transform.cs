using llmc.Project;
using llmc.Repl;
using System.Reflection;
using System.Text;

namespace llmc.Features;

internal class Transform : FeatureCommon
{
    public override bool AsPrebuild => false;

    public override FeatureResult Execute(string parentDirectory, string param)
    {
        Console.WriteLine("Executing feature Transform: " + param);

        StringBuilder undo = new();

        EnsureThat.EnsureArg.IsNotNull(Storage);
        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));
        EnsureThat.EnsureArg.IsNotNull(Prompt, nameof(Prompt));

        Dictionary<string, string> p = Common.ParseParam(param);
        string? inputtextpath = p.ContainsKey("inputtextpath") ? p["inputtextpath"] : null;
        string? inputlinespath = p.ContainsKey("inputlinespath") ? p["inputlinespath"] : null;
        string? wildcard = p.ContainsKey("wildcard") ? p["wildcard"] : null;
        string? outputpath = p.ContainsKey("outputpath") ? p["outputpath"] : null;
        string? outputfile = p.ContainsKey("outputfile") ? p["outputfile"] : null;
        string? outputappendfile = p.ContainsKey("outputappendfile") ? p["outputappendfile"] : null;

        string promptText = Prompt.Text;

        // Clean the prompt text to avoid further consumption.
        Prompt.Text = string.Empty;

        // Run script.
        CSharpRepl cSharpRepl = new();

        // Conditions.
        var contentEnumerable = GetInputEnumerable(
            parentDirectory, inputtextpath, inputlinespath, wildcard);

        // Execute.
        foreach (var (fpath, content) in contentEnumerable)
        {
            // Execute.
            ScriptResult result = cSharpRepl.Execute(
                parentDirectory,
                promptText,
                Storage,
                [(nameof(fpath), fpath), ("value", content)],
                [],
                []);

            // Save.
            SaveOutput(fpath, result, parentDirectory, outputpath, outputfile, outputappendfile);
        }

        return new FeatureResult();
    }

    private void SaveOutput(
        string inputfpath, ScriptResult result, string parentDirectory,
        string? outputpath, string? outputfile, string? outputappendfile)
    {
        EnsureThat.EnsureArg.IsNotNull(Storage);

        if (!string.IsNullOrEmpty(outputpath))
        {
            if (!Storage.Exists(Path.Combine(parentDirectory, outputpath)))
            {
                Storage.CreateDirectory(Path.Combine(parentDirectory, outputpath));
            }

            string path = Path.Combine(parentDirectory, outputpath, Path.GetFileName(inputfpath));

            if (!Storage.Exists(path))
            {
                Storage.WriteAllText(path, string.Empty);
            }

            Storage.AppendAllLines(path, [result.Result]);
        }
        else if (!string.IsNullOrEmpty(outputfile))
        {
            string path = Path.Combine(parentDirectory, outputfile);

            string outputDirectory = Path.GetDirectoryName(path)
                ?? throw new Exception("Error in getting output directory.");

            if (!Storage.Exists(outputDirectory))
            {
                Storage.CreateDirectory(outputDirectory);
            }

            Storage.WriteAllText(path, result.Result);
        }
        else if (!string.IsNullOrEmpty(outputappendfile))
        {
            string path = Path.Combine(parentDirectory, outputappendfile);

            string outputDirectory = Path.GetDirectoryName(path)
                ?? throw new Exception("Error in getting output directory.");

            if (!Storage.Exists(outputDirectory))
            {
                Storage.CreateDirectory(outputDirectory);
            }

            if (!Storage.Exists(path))
            {
                Storage.WriteAllText(path, string.Empty);
            }

            Storage.AppendAllLines(path, [result.Result]);
        }
        else
        {
            Console.WriteLine("Transform: No output specified.");
        }
    }

    private IEnumerable<(string fpath, string content)> GetInputEnumerable(
        string parentDirectory, string? inputtextpath, string? inputlinespath, string? wildcard)
    {
        EnsureThat.EnsureArg.IsNotNull(Storage);

        if (!string.IsNullOrEmpty(inputtextpath))
        {
            string path = Path.Combine(parentDirectory, inputtextpath);
            
            foreach (var fpath in Storage.GetFiles(path, wildcard ?? "*"))
            {
                yield return (fpath, Storage.ReadAllText(fpath));
            }
        }
        else if (!string.IsNullOrEmpty(inputlinespath))
        {
            string path = Path.Combine(parentDirectory, inputlinespath);

            foreach (var fpath in Storage.GetFiles(path, wildcard ?? "*"))
            {
                foreach (var line in Storage.ReadAllLines(fpath))
                {
                    yield return (fpath, line);
                }
            }
        }
        else
        {
            Console.WriteLine("Transform: No input specified.");
        }
    }
}
