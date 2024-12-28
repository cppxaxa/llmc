namespace llmc.Project;

public class Metadata
{
    public List<string> Prompt { get; set; } = [];
    public List<string> PreBuild { get; set; } = [];
    public List<string> PostBuild { get; set; } = [];
    public List<string> AppendToCleanup { get; set; } = [];
    public List<string> Features { get; set; } = [];
    public List<string> Flags { get; set; } = [];
}