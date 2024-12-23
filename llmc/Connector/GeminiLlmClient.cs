
using Newtonsoft.Json;
using System.Text;

namespace llmc.Connector;

internal class GeminiLlmClient(Configuration configuration) : ILlmClient
{
    public string Complete(string prompt)
    {
        if (configuration.EnableGemini)
        {
            string key = Environment.GetEnvironmentVariable(configuration.ApiKeyEnvVar)!;
            string url = Environment.GetEnvironmentVariable(configuration.GeminiUrlEnvVar)
                ?? configuration.Url;

            url = url.Replace("{geminikey}", key);

            // Get HTTP client.
            var client = new HttpClient();

            // Build the body.
            string body = JsonConvert.SerializeObject(new
            {
                contents = new object[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                }
            });

            // Set the prompt as body.
            var content = new StringContent(body, Encoding.UTF8, "application/json");

            // Send the request.
            var response = client.PostAsync(url, content).Result;

            // Read the response.
            var responseContent = response.Content.ReadAsStringAsync().Result;

            // Deserialize the response.
            var responseObj = JsonConvert.DeserializeObject<GeminiCompletionResult>(responseContent);

            return responseObj!.Candidates[0].Content.Parts[0].Text;
        }

        return string.Empty;
    }
}

public record GeminiCompletionResult(
    List<GeminiCandidate> Candidates,
    GeminiUsageMetadata UsageMetadata,
    string ModelVersion
);

public record GeminiCandidate(
    GeminiContent Content,
    string FinishReason,
    GeminiCitationMetadata CitationMetadata,
    double AvgLogprobs
);

public record GeminiContent(
    List<GeminiPart> Parts,
    string Role
);

public record GeminiPart(
    string Text
);

public record GeminiCitationMetadata(
    List<GeminiCitationSource> CitationSources
);

public record GeminiCitationSource(
    int EndIndex,
    string Uri
);

public record GeminiUsageMetadata(
    int PromptTokenCount,
    int CandidatesTokenCount,
    int TotalTokenCount
);
