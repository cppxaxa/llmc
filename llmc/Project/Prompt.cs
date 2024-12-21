namespace llmc.Project;

public class Prompt
{
    public string Text { get; set; }
    public string MetadataYaml { get; init; }
    public List<ExecutorFinderResult> PreBuild { get; init; }
    public List<ExecutorFinderResult> Features { get; init; }
    public List<ExecutorFinderResult> PostBuild { get; init; }
    public Metadata Metadata { get; init; }

    public Prompt(
        string text, string metadataYaml, List<ExecutorFinderResult> preBuild,
        List<ExecutorFinderResult> features, List<ExecutorFinderResult> postBuild,
        Metadata metadata)
    {
        Text = text;
        MetadataYaml = metadataYaml;
        PreBuild = preBuild;
        Features = features;
        PostBuild = postBuild;
        Metadata = metadata;
    }
}
