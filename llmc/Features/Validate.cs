using llmc.Project;
using llmc.Repl;
using llmc.Storage;
using System.Reflection;
using System.Text;

namespace llmc.Features;

internal class Validate : FeatureCommon
{
    public override bool AsPrebuild => false;

    public override FeatureResult Execute(string parentDirectory, string param)
    {
        Console.WriteLine("Executing feature Validate: " + param);

        StringBuilder undo = new();

        EnsureThat.EnsureArg.IsNotNull(Storage);
        EnsureThat.EnsureArg.IsNotNull(Prompt, nameof(Prompt));

        Dictionary<string, string> p = Common.ParseParam(param);
        string? errorgoto = p.ContainsKey("errorgoto") ? p["errorgoto"] : null;

        string promptText = Prompt.Text;

        // Clean the prompt text to avoid further consumption.
        Prompt.Text = string.Empty;

        // Run script.
        CSharpRepl cSharpRepl = new();

        // Execute.
        ScriptResult result = cSharpRepl.Execute(
            parentDirectory,
            promptText,
            Storage,
            [],
            [],
            []);

        if (VerboseLogging)
        {
            Console.WriteLine("Result: " + result.Result);
            Console.WriteLine("IsError: " + result.IsError);
        }

        string? gotoPromptsAfter = null;

        if (bool.TryParse(result.Result, out bool isResultError) && !isResultError)
        {
            gotoPromptsAfter = errorgoto;
        }
        else if (result.IsError)
        {
            gotoPromptsAfter = errorgoto;
        }

        Console.WriteLine($"Validate:GotoPromptsAfter: {gotoPromptsAfter}");

        return new FeatureResult(GotoPromptsAfter: gotoPromptsAfter);
    }
}
