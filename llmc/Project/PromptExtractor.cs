
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace llmc.Project;

internal class PromptExtractor
{
    internal Prompt Extract(string rawPrompt)
    {
        string newLineSeparator = Common.FindLineSeparator(rawPrompt);
        List<string> separators = [];

        foreach (var line in rawPrompt
            .Split(newLineSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Trim().Trim('-').Length == 0)
            {
                separators.Add(line);
            }
        }

        var sections = rawPrompt.Split(separators.ToArray(), StringSplitOptions.None);

        string metadataYaml = string.Empty;

        if (sections.Length > 1)
        {
            metadataYaml = sections[0];
        }

        // Create a deserializer.
        var deserializer = new DeserializerBuilder().Build();

        // Deserialize the object graph from the YAML document.
        var metadata = deserializer.Deserialize<Metadata>(metadataYaml);

        // Parse prebuild.
        List<ExecutorFinderResult> preBuild = [];

        if (metadata.PreBuild != null)
        {
            foreach (string preBuildItem in metadata.PreBuild)
            {
                (string className, string param) = ExecutorParser.Parse(preBuildItem);

                preBuild.Add(new ExecutorFinderResult(ClassName: className, Param: param));
            }
        }

        // Parse postbuild.
        List<ExecutorFinderResult> postBuild = [];

        if (metadata.PostBuild != null)
        {
            foreach (string postBuildItem in metadata.PostBuild)
            {
                (string className, string param) = ExecutorParser.Parse(postBuildItem);

                postBuild.Add(new ExecutorFinderResult(ClassName: className, Param: param));
            }
        }

        return new Prompt(
            Text: sections.LastOrDefault(string.Empty), MetadataYaml: metadataYaml,
            PreBuild: preBuild, PostBuild: postBuild, Metadata: metadata);
    }
}