namespace llmc.Connector;

internal record Configuration(
    bool EnabledGemini, string GeminiKeyEnvVar, string GeminiUrlEnvVar, string GeminiUrl);
