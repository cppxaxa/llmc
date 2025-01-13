using llmc.Project;
using llmc.Repl;
using System.IO;
using System.Reflection;
using System.Text;

namespace llmc.Features;

internal class WriteFile : FeatureCommon
{
    public override bool AsPrebuild => false;

    public override FeatureResult Execute(string parentDirectory, string param)
    {
        Console.WriteLine("Executing feature WriteFile: " + param);

        StringBuilder undo = new();

        EnsureThat.EnsureArg.IsNotNull(Storage);
        EnsureThat.EnsureArg.IsNotNull(Prompt, nameof(Prompt));

        Dictionary<string, string> p = Common.ParseParam(param);
        string? source = p.ContainsKey("source") ? p["source"] : "prompt";
        string? file = p.ContainsKey("file") ? p["file"] : null;

        EnsureThat.EnsureArg.IsNotNull(file, nameof(file));

        string promptText = Prompt.Text;

        // Clean the prompt text to avoid further consumption.
        Prompt.Text = string.Empty;

        string content;

        // Save.
        if (source == "prompt")
        {
            content = promptText;
        }
        else
        {
            content = ReadFromStdin(file);
        }
        
        SaveOutput(file, parentDirectory, content);

        return new FeatureResult();
    }

    private string ReadFromStdin(string file)
    {
        StringBuilder stringBuilder = new();

        string responseStart = $"<response>";
        string responseEnd = $"</response>";

        stringBuilder.AppendLine($"## Respond back text inside " +
            $"{responseStart} and {responseEnd} tags.");
        stringBuilder.AppendLine($"<readfile>");
        stringBuilder.AppendLine($"filepath:{file}");
        stringBuilder.AppendLine($"</readfile>");
        
        Console.WriteLine(stringBuilder.ToString());

        // Parse stdin buffer.
        StringBuilder response = new();

        while (!response.ToString().Trim().Contains(responseStart) ||
            !response.ToString().Trim().Contains(responseEnd))
        {
            int ch = Console.Read();

            if (ch >= 0) response.Append((char)ch);
        }

        string responseString = response.ToString();

        // Extract the response content.
        string content = responseString.Substring(
            responseString.IndexOf(responseStart) + responseStart.Length,
            responseString.IndexOf(responseEnd) - responseString.IndexOf(responseStart) - responseStart.Length);

        return content;
    }

    private void SaveOutput(string inputfpath, string parentDirectory, string content)
    {
        EnsureThat.EnsureArg.IsNotNull(Storage);

        string path = Path.Combine(parentDirectory, inputfpath);
        Storage.WriteAllText(path, content);
    }
}
