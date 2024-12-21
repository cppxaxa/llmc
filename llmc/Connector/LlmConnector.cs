

namespace llmc.Connector;

internal class LlmConnector(Configuration configuration)
{
    public string Complete(string prompt)
    {
        var client = GetClient();
        return client.Complete(prompt);
    }

    public float[]? GetEmbedding(string text)
    {
        var client = GetClient();
        return client.GetEmbedding(text);
    }

    private Client GetClient()
    {
        return new Client(configuration);
    }
}
