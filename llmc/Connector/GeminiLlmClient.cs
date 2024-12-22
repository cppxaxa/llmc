
using Newtonsoft.Json;
using System.Text;

namespace llmc.Connector;

internal class GeminiLlmClient(Configuration configuration)
{
    internal string Complete(string prompt)
    {
        if (configuration.EnableGemini)
        {
            string key = Environment.GetEnvironmentVariable(configuration.ApiKeyEnvVar)!;
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
}