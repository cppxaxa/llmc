using llmc.Storage;

namespace llmc.Project;

public class Prompt
{
    public string Text { get; set; }
    public string MetadataYaml { get; init; }
    public List<ExecutorFinderResult> PreBuild { get; init; }
    public List<ExecutorFinderResult> Features { get; init; }
    public List<ExecutorFinderResult> PostBuild { get; init; }
    public HashSet<Flag> Flags { get; init; }
    public StorageConfiguration StorageConfiguration { get; set; }
    public Metadata Metadata { get; init; }

    public Prompt(
        string text, string metadataYaml, List<ExecutorFinderResult> preBuild,
        List<ExecutorFinderResult> features, List<ExecutorFinderResult> postBuild,
        HashSet<Flag> flags, StorageConfiguration storageConfiguration, Metadata metadata)
    {
        Text = text;
        MetadataYaml = metadataYaml;
        PreBuild = preBuild;
        Features = features;
        PostBuild = postBuild;
        Flags = flags;
        StorageConfiguration = storageConfiguration;
        Metadata = metadata;
    }
}
