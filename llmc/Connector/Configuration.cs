namespace llmc.Connector;

internal record Configuration(
    ConfigurationType Type, bool EnabledGemini, string GeminiKeyEnvVar, string GeminiUrlEnvVar = "", string GeminiUrl = "", string GeminiEmbeddingUrl = "");
