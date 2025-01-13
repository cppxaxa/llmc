
using LlmcConsumer.Connector;
using LlmcConsumer.LlmcWrapper;
using Newtonsoft.Json;

Console.WriteLine("LlmcConsumer starting...");

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

string funcGetFile(string file)
{
    if (file == "presetsvector-search/search-result.jsonl")
    {
        return File.ReadAllText(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Data/presetshortlist-search-result.jsonl"));
    }
    else
    {
        return $"No content available for {file}.";
    }
}

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

Console.WriteLine("LlmcConsumer: Sample query: Tell me ad clicks for China and US market from PCT and it should be Shopping ad campaign type");
Console.WriteLine("LlmcConsumer: Enter chart query");

string? userQuery = Console.ReadLine();

if (string.IsNullOrWhiteSpace(userQuery))
{
    userQuery = "Tell me ad clicks for China and US market from PCT and it should be Shopping ad campaign type";
    Console.WriteLine("LlmcConsumer: Using default query: " + userQuery);
}

string projectJson = JsonConvert.SerializeObject(new
{
    macros = new Dictionary<string, string>
    {
        ["{{{chartquery}}}"] = userQuery
    }
});

string[] projectCwdList = [
    //"C:\\B\\L1\\llmc\\playground\\project-presetsearch_real",
    //"C:\\B\\L1\\llmc\\playground\\project-dimensionsearch_real",
    "C:\\B\\L1\\llmc\\playground\\project-presetshortlist_real"
];

ConsoleColor color;

foreach (var projectCwd in projectCwdList)
{
    color = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("LlmcConsumer: Running LLMC for project in " + projectCwd);
    Console.ForegroundColor = color;

    var result = llmc.Execute(
        passthroughStdout: false, printLlmResult: false,
        projectCwd: projectCwd, projectJson: projectJson,
        funcLlm: funcLlm, funcEmbedding: funcEmbedding,
        funcGetFile: funcGetFile);

    Console.WriteLine("LLMC output:");

    if (result.ConsoleWriteline.ContainsKey("shortlistpresets"))
    {
        color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Shortlist presets:");
        Console.ForegroundColor = color;
        Console.WriteLine(result.ConsoleWriteline["shortlistpresets"]);
        Console.WriteLine();
    }

    if (result.ConsoleWriteline.ContainsKey("user-prompt-without-presets"))
    {
        color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("User prompt without presets:");
        Console.ForegroundColor = color;
        Console.WriteLine(result.ConsoleWriteline["user-prompt-without-presets"]);
        Console.WriteLine();
    }

    if (result.ConsoleWriteline.ContainsKey("dimensions"))
    {
        color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Dimensions:");
        Console.ForegroundColor = color;
        Console.WriteLine(result.ConsoleWriteline["dimensions"]);
        Console.WriteLine();
    }

    //if (result.ConsoleWriteline.ContainsKey("presetspool"))
    //{
    //    color = Console.ForegroundColor;
    //    Console.ForegroundColor = ConsoleColor.Green;
    //    Console.WriteLine("Presets pool:");
    //    Console.ForegroundColor = color;
    //    Console.WriteLine(result.ConsoleWriteline["presetspool"]);
    //    Console.WriteLine();
    //}

    //if (result.ConsoleWriteline.ContainsKey("dimensionssearchresult"))
    //{
    //    color = Console.ForegroundColor;
    //    Console.ForegroundColor = ConsoleColor.Cyan;
    //    Console.WriteLine("Dimensions search result:");
    //    Console.ForegroundColor = color;
    //    Console.WriteLine(result.ConsoleWriteline["dimensionssearchresult"]);
    //    Console.WriteLine();
    //}
}