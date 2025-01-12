namespace llmc.Project;

internal record MultipleFeatureResult(
    bool AnyFeatureProcessed,
    string? GotoPromptsAfter,
    int? MaxRetry);