
namespace llmc.Connector;

internal class LlmConnector(List<Configuration> configurations)
{
    private Dictionary<ConfigurationType, Configuration> configurationCache = [];

    public string Complete(string prompt)
    {
        var client = GetLlmClient();
        return client.Complete(prompt);
    }

    public float[]? GetEmbedding(string text)
    {
        var client = GetEmbeddingClient();
        return client.GetEmbedding(text);
    }

    private ILlmClient GetLlmClient()
    {
        if (!configurationCache.ContainsKey(ConfigurationType.Llm))
            configurationCache.Add(
                ConfigurationType.Llm,
                configurations.First(e => e.Type == ConfigurationType.Llm));

        if (configurationCache[ConfigurationType.Llm].EnableGemini)
        {
            return new GeminiLlmClient(configurationCache[ConfigurationType.Llm]);
        }
        else
        {
            return new AoaiLlmClient(configurationCache[ConfigurationType.Llm]);
        }
    }

    private IEmbeddingClient GetEmbeddingClient()
    {
        if (!configurationCache.ContainsKey(ConfigurationType.Embedding))
            configurationCache.Add(
                ConfigurationType.Embedding,
                configurations.First(e => e.Type == ConfigurationType.Embedding));

        if (configurationCache[ConfigurationType.Embedding].EnableGemini)
        {
            return new GeminiEmbeddingClient(
                configurationCache[ConfigurationType.Embedding]);
        }
        else
        {
            return new AoaiEmbeddingClient(
                configurationCache[ConfigurationType.Embedding]);
        }
    }
}
