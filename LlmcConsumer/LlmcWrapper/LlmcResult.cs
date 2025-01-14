namespace LlmcConsumer.LlmcWrapper;

public record LlmcResult(Dictionary<string, ConsoleWritelineModel> ConsoleWriteline);

public class ConsoleWritelineModel
{
    public string RawContent { get; }
    public string FilePath { get; } = string.Empty;
    public string Exists { get; } = string.Empty;
    public int NLines { get; } = -1;

    public ConsoleWritelineModel(string content)
    {
        RawContent = content;

        string lineSeparator = Common.FindLineSeparator(RawContent);
        List<string> lines = [];

        foreach (string line in RawContent.Split(lineSeparator))
        {
            if (line.StartsWith("## "))
            {
                if (line.StartsWith("## FullPath:"))
                {
                    FilePath = line.Substring("## FullPath:".Length);
                }
                else if (line.StartsWith("## Exists:"))
                {
                    Exists = line.Substring("## Exists:".Length);
                }
                else if (line.StartsWith("## NLines:"))
                {
                    NLines = int.Parse(line.Substring("## NLines:".Length));
                }
            }
            else
            {
                break;
            }
        }
    }

    public string GetBody()
    {
        string lineSeparator = Common.FindLineSeparator(RawContent);
        List<string> lines = [];

        foreach (string line in RawContent.Split(lineSeparator))
        {
            if (line.StartsWith("## "))
            {
                continue;
            }

            lines.Add(line);
        }

        return string.Join(lineSeparator, lines);
    }
}