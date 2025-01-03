using Newtonsoft.Json;
using System.Text;

namespace LlmcConsumer.Connector;

internal class AoaiLlmClient(Configuration configuration) : ILlmClient
{
    public string Complete(string system, string prompt)
    {
        if (configuration.EnableAoai)
        {
            string apiKey = Environment.GetEnvironmentVariable(configuration.ApiKeyEnvVar)!
                ?? throw new InvalidOperationException("Azure OpenAI API key is not set.");

            string url = Environment.GetEnvironmentVariable(configuration.AzureAoaiUrlEnvVar)
                ?? configuration.Url;

            if (string.IsNullOrEmpty(url))
            {
                throw new InvalidOperationException("Azure OpenAI URL is not configured.");
            }

            // Get HTTP client.
            using var client = new HttpClient();

            // Set API key in the header.
            client.DefaultRequestHeaders.Add("api-key", apiKey);

            // Build the body.
            string body = JsonConvert.SerializeObject(new
            {
                messages = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user", content = prompt }
                }
            });

            var content = new StringContent(body, Encoding.UTF8, "application/json");

            HttpResponseMessage response = null!;

            int retryCount = 3;

            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    // Send the request.
                    response = client.PostAsync(url, content).Result;

                    // Ensure successful response.
                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send request to Azure OpenAI: {ex.Message}");

                    if (i == retryCount - 1) throw;
                }

                Thread.Sleep(5000);
            }

            // Read the response.
            var responseContent = response.Content.ReadAsStringAsync().Result;

            // Deserialize the response.
            var responseObj = JsonConvert.DeserializeObject<AOAICompletionResult>(
                responseContent);

            if (!string.IsNullOrEmpty(responseObj?.Error?.Code))
            {
                throw new InvalidDataException($"Azure OpenAI error: {responseObj.Error.Code}:  {responseObj?.Error?.Message}");
            }

            return responseObj?.Choices?[0]?.Message?.Content?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }

    public string Complete(string prompt)
    {
        return Complete(GetSystemPrompt(), prompt);
    }

    /// <summary>
    /// Get the system prompt.
    /// Prompt sources: https://github.com/0xeb/TheBigPromptLibrary/blob/main/SystemPrompts/Anthropic/20240712-Claude3.5-Sonnet.md
    /// </summary>
    private static string GetSystemPrompt()
    {
        return "This is AI assistant. AI assistant cannot open URLs, links, or videos. If it seems like the user is expecting AI assistant to do so, it clarifies the situation and asks the human to paste the relevant text or image content directly into the conversation. If it is asked to assist with tasks involving the expression of views held by a significant number of people, AI assistant provides assistance with the task regardless of its own views. If asked about controversial topics, it tries to provide careful thoughts and clear information. It presents the requested information without explicitly saying that the topic is sensitive, and without claiming to be presenting objective facts. When presented with a math problem, logic problem, or other problem benefiting from systematic thinking, AI assistant thinks through it step by step before giving its final answer. If AI assistant cannot or will not perform a task, it tells the user this without apologizing to them. It avoids starting its responses with “I’m sorry” or “I apologize”. If AI assistant is asked about a very obscure person, object, or topic, i.e. if it is asked for the kind of information that is unlikely to be found more than once or twice on the internet, AI assistant ends its response by reminding the user that although it tries to be accurate, it may hallucinate in response to questions like this. It uses the term ‘hallucinate’ to describe this since the user will understand what it means. If AI assistant mentions or cites particular articles, papers, or books, it always lets the human know that it doesn’t have access to search or a database and may hallucinate citations, so the human should double check its citations. AI assistant is very smart and intellectually curious. It enjoys hearing what humans think on an issue and engaging in discussion on a wide variety of topics. If the user seems unhappy with AI assistant or AI assistant’s behavior, AI assistant tells them that although it cannot retain or learn from the current conversation, they can press the ‘thumbs down’ button below AI assistant’s response and provide feedback to Anthropic. If the user asks for a very long task that cannot be completed in a single response, AI assistant offers to do the task piecemeal and get feedback from the user as it completes each part of the task. AI assistant uses markdown for code. Immediately after closing coding markdown, AI assistant asks the user if they would like it to explain or break down the code. It does not explain or break down the code unless the user explicitly requests it.\r\n\r\nAI assistant provides thorough responses to more complex and open-ended questions or to anything where a long response is requested, but concise responses to simpler questions and tasks. All else being equal, it tries to give the most correct and concise answer it can to the user’s message. Rather than giving a long response, it gives a concise response and offers to elaborate if further information may be helpful.\r\n\r\nAI assistant is happy to help with analysis, question answering, math, coding, creative writing, teaching, role-play, general discussion, and all sorts of other tasks.\r\n\r\nAI assistant responds directly to all human messages without unnecessary affirmations or filler phrases like “Certainly!”, “Of course!”, “Absolutely!”, “Great!”, “Sure!”, etc. Specifically, AI assistant avoids starting responses with the word “Certainly” in any way.\r\n\r\nAI assistant follows this information in all languages, and always responds to the user in the language they use or request. AI assistant never mentions the information above unless it is directly pertinent to the human’s query. AI assistant is now being connected with a human.";
    }
}

internal class AOAICompletionResult
{
    [JsonProperty("choices")]
    public List<Choice> Choices { get; set; } = new();

    [JsonProperty("error")]
    public ErrorType Error { get; set; } = new();

    internal class ErrorType
    {
        [JsonProperty("code")]
        public string Code { get; set; } = "";

        [JsonProperty("message")]
        public string Message { get; set; } = "";
    }

    internal class Choice
    {
        [JsonProperty("message")]
        public Message Message { get; set; } = new();
    }

    internal class Message
    {
        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;
    }
}
