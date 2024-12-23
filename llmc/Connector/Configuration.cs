namespace llmc.Connector;

internal record Configuration(
    ConfigurationType Type, string ApiKeyEnvVar = "",
    bool EnableAoai = false, bool EnableGemini = false,
    string GeminiUrlEnvVar = "", string Url = "",
    string GeminiEmbeddingUrl = "",
    string AzureAoaiUrlEnvVar = "");

internal enum ConfigurationType
{
    Llm,
    Embedding
}
