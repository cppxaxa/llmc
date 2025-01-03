using Newtonsoft.Json;
using System.Text;

namespace LlmcConsumer.Connector;

internal class AoaiEmbeddingClient(Configuration configuration) : IEmbeddingClient
{
    public float[]? GetEmbedding(string input)
    {
        if (configuration.EnableAoai)
        {
            string apiKey = Environment.GetEnvironmentVariable(configuration.ApiKeyEnvVar)
                ?? throw new InvalidOperationException("API key environment variable is not set.");

            string targetUrl = configuration.Url
                ?? throw new InvalidOperationException("AOAI target URL is not configured.");

            // Get HTTP client.
            using var client = new HttpClient();

            // Set headers.
            client.DefaultRequestHeaders.Add("api-key", apiKey);

            // Build the body.
            string body = JsonConvert.SerializeObject(new
            {
                input = input
            });

            var content = new StringContent(body, Encoding.UTF8, "application/json");

            // Send the request.
            var response = client.PostAsync(targetUrl, content).Result;

            // Ensure the request was successful.
            response.EnsureSuccessStatusCode();

            // Read the response.
            var responseContent = response.Content.ReadAsStringAsync().Result;

            // Parse the response and extract the embedding.
            var responseObj = JsonConvert.DeserializeObject<EmbeddingResponse>(responseContent);
            
            return responseObj?.Data?.FirstOrDefault()?.Embedding
                ?? throw new InvalidOperationException(
                    "Failed to extract embedding from the response.");
        }

        throw new InvalidOperationException("AOAI is not enabled in the configuration.");
    }

    private class EmbeddingResponse
    {
        public string Object { get; set; } = string.Empty;
        public List<EmbeddingData> Data { get; set; } = [];
        public string Model { get; set; } = string.Empty;
        public UsageInfo Usage { get; set; } = new();
    }

    private class EmbeddingData
    {
        public string Object { get; set; } = string.Empty;
        public int Index { get; set; }
        public float[] Embedding { get; set; } = [];
    }

    private class UsageInfo
    {
        public int PromptTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
