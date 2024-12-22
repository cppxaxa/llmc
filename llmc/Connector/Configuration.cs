namespace llmc.Connector;

internal record Configuration(
    ConfigurationType Type, string ApiKeyEnvVar = "", bool EnableAoai = false, bool EnableGemini = false, string GeminiUrlEnvVar = "", string GeminiUrl = "", string AoaiTargetUrl = "", string GeminiEmbeddingUrl = "");
