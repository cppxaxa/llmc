namespace llmc.Project;

public record Prompt(
    string Text, string MetadataYaml,
    List<ExecutorFinderResult> PreBuild, List<ExecutorFinderResult> Features,
    List<ExecutorFinderResult> PostBuild, Metadata Metadata);