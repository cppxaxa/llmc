namespace llmc.Features;

internal record FeatureResult(
    bool Executed = true, string? GotoPromptsAfter = null, int? MaxRetry = null);
