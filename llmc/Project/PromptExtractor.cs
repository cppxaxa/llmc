﻿
using llmc.Storage;
using YamlDotNet.Serialization;

namespace llmc.Project;

internal class PromptExtractor
{
    internal Prompt Extract(string filename, string rawPrompt)
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

        // Parse features.
        List<ExecutorFinderResult> features = [];

        if (metadata.Features != null)
        {
            foreach (string featureItem in metadata.Features)
            {
                (string className, string param) = ExecutorParser.Parse(featureItem);

                features.Add(new ExecutorFinderResult(ClassName: className, Param: param));
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

        // Parse flags.
        HashSet<Flag> flags = new();

        if (metadata.Flags != null)
        {
            foreach (string flagItem in metadata.Flags)
            {
                if (Enum.TryParse<Flag>(flagItem, out var flag))
                {
                    flags.Add(flag);
                }
                else
                {
                    Console.WriteLine($"PromptExtractor: Unknown flag {flagItem}");
                }
            }
        }

        // Create storage configuration.
        StorageConfiguration storageConfiguration = new(
            EnableInMemoryStorage: flags.Contains(Flag.EnableInMemoryStorage));

        return new Prompt(
            filename: filename,
            text: sections.LastOrDefault(string.Empty), metadataYaml: metadataYaml,
            preBuild: preBuild, features: features, postBuild: postBuild,
            flags: flags, storageConfiguration: storageConfiguration, metadata: metadata);
    }
}
