namespace llmc.Connector;

public record Result(
    List<Candidate> Candidates,
    UsageMetadata UsageMetadata,
    string ModelVersion
);

public record Candidate(
    Content Content,
    string FinishReason,
    CitationMetadata CitationMetadata,
    double AvgLogprobs
);

public record Content(
    List<Part> Parts,
    string Role
);

public record Part(
    string Text
);

public record CitationMetadata(
    List<CitationSource> CitationSources
);

public record CitationSource(
    int EndIndex,
    string Uri
);

public record UsageMetadata(
    int PromptTokenCount,
    int CandidatesTokenCount,
    int TotalTokenCount
);
