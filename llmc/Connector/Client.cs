
using Newtonsoft.Json;
using System.Text;

namespace llmc.Connector;

internal class Client(Configuration configuration)
{
    internal string Complete(string prompt)
    {
        if (configuration.EnabledGemini)
        {
            string key = Environment.GetEnvironmentVariable(configuration.GeminiKeyEnvVar)!;
            string url = Environment.GetEnvironmentVariable(configuration.GeminiUrlEnvVar)
                ?? configuration.GeminiUrl;

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
            var responseObj = JsonConvert.DeserializeObject<CompletionResult>(responseContent);

            return responseObj!.Candidates[0].Content.Parts[0].Text;
        }

        return string.Empty;
    }

    internal float[]? GetEmbedding(string text)
    {
        if (configuration.EnabledGemini)
        {
            try
            {
                // Fetch API key and URL.
                string key = Environment.GetEnvironmentVariable(configuration.GeminiKeyEnvVar)!;
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