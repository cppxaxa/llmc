namespace LlmcConsumer.Connector;

internal interface IEmbeddingClient
{
    float[]? GetEmbedding(string text);
}
