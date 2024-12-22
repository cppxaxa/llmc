namespace llmc.Connector;

internal interface IEmbeddingClient
{
    float[]? GetEmbedding(string text);
}
