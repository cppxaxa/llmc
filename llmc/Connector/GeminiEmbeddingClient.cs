
using Newtonsoft.Json;
using System.Text;

namespace llmc.Connector;

internal class GeminiEmbeddingClient(Configuration configuration) : IEmbeddingClient
{
    public float[]? GetEmbedding(string text)
    {
        if (configuration.EnableGemini)
        {
            try
            {
                // Fetch API key and URL.
                string key = Environment.GetEnvironmentVariable(configuration.ApiKeyEnvVar)!;
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key={key}";

                // Initialize HTTP client.
                using var client = new HttpClient();

                // Build the request body.
                var requestBody = new
                {
                    model = "models/text-embedding-004",
                    content = new
                    {
                        parts = new[]
                        {
                            new { text }
                        }
                    }
                };

                string body = JsonConvert.SerializeObject(requestBody);

                // Prepare the HTTP content.
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                // Send the request synchronously.
                var response = client.PostAsync(url, content).Result;

                // Ensure the response is successful.
                response.EnsureSuccessStatusCode();

                // Parse the response content.
                var responseContent = response.Content.ReadAsStringAsync().Result;

                // Deserialize the embedding result.
                var responseObj = JsonConvert.DeserializeObject<EmbeddingResult>(responseContent);

                return responseObj?.Embedding.Values;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately.
                Console.Error.WriteLine($"Error fetching embedding: {ex.Message}");
                return null;
            }
        }

        return null;
    }
}

internal record EmbeddingResult(EmbeddingData Embedding);

internal record EmbeddingData(float[] Values);