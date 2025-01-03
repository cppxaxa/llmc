
using LlmcConsumer.Connector;
using LlmcConsumer.LlmcWrapper;
using Newtonsoft.Json;

Console.WriteLine("LlmConsumer starting...");

var geminiLlmConfiguration = new Configuration(
    Type: ConfigurationType.Llm,
    EnableGemini: true,
    ApiKeyEnvVar: "GEMINI_API_KEY",
    Url: "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={geminikey}");

var aoaiLlmConfiguration = new Configuration(
    Type: ConfigurationType.Llm,
    EnableAoai: true,
    ApiKeyEnvVar: "AOAI_API_KEY",
    Url: "https://icanazopenai.openai.azure.com/openai/deployments/gpt-4o-mini/chat/completions?api-version=2024-10-21");

var geminiEmbeddingConfiguration = new Configuration(
    Type: ConfigurationType.Embedding,
    EnableGemini: true,
    ApiKeyEnvVar: "GEMINI_API_KEY",
    GeminiEmbeddingUrl: "https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key={geminikey}");

var aoaiEmbeddingConfiguration = new Configuration(
    Type: ConfigurationType.Embedding,
    EnableAoai: true,
    ApiKeyEnvVar: "AOAI_API_KEY",
    Url: "https://icanazopenai.openai.azure.com/openai/deployments/text-embedding-ada-002/embeddings?api-version=2023-05-15");

var llmConnector = new LlmConnector([aoaiLlmConfiguration, aoaiEmbeddingConfiguration]);

string funcLlm(string system, string prompt)
{
    return llmConnector.Complete(system, prompt);
}

string funcEmbedding(string prompt)
{
    return JsonConvert.SerializeObject(llmConnector.GetEmbedding(prompt));
}

Llmc llmc = new(new LlmcConfiguration(
    azurellm: false, azureembedding: false, noundo: true));

Console.WriteLine("LlmConsumer: Sample query: Tell me ad clicks for China and US market from PCT and it should be Shopping ad");
Console.WriteLine("LlmConsumer: Enter chart query");

string? userQuery = Console.ReadLine();

if (string.IsNullOrWhiteSpace(userQuery))
{
    userQuery = "Tell me ad clicks for China and US market from PCT and it should be Shopping ad";
    Console.WriteLine("LlmConsumer: Using default query: " + userQuery);
}

string projectJson = JsonConvert.SerializeObject(new
{
    macros = new Dictionary<string, string>
    {
        ["{{{chartquery}}}"] = userQuery
    }
});

string projectCwd = "C:\\B\\L1\\llmc\\playground";

var result = llmc.Execute(
    passthroughStdout: true, printLlmResult: true,
    projectCwd: projectCwd, projectJson: projectJson,
    funcLlm: funcLlm, funcEmbedding: funcEmbedding);

Console.WriteLine("LLMC output:");

if (result.ConsoleWriteline.ContainsKey("shortlistpresets"))
{
    Console.WriteLine(result.ConsoleWriteline["shortlistpresets"]);
}

if (result.ConsoleWriteline.ContainsKey("user-prompt-without-presets"))
{
    Console.WriteLine(result.ConsoleWriteline["user-prompt-without-presets"]);
}

if (result.ConsoleWriteline.ContainsKey("dimensions"))
{
    Console.WriteLine(result.ConsoleWriteline["dimensions"]);
}
