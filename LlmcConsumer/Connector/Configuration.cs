namespace LlmcConsumer.Connector;

internal record Configuration(
    ConfigurationType Type, string ApiKeyEnvVar = "",
    bool EnableAoai = false, bool EnableGemini = false,
    string GeminiUrlEnvVar = "", string Url = "",
    string GeminiEmbeddingUrl = "",
    string AzureAoaiUrlEnvVar = "",
    bool EnableStdStream = false);

internal enum ConfigurationType
{
    Llm,
    Embedding
}
