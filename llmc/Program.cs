
using llmc.Connector;
using llmc.Project;

if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h" || args[0] == "/?"))
{
    Console.WriteLine("* It looks for *.llmc.json as a project file. There should be only one such file in the directory.");
    Console.WriteLine("* It generates undo.executor.txt for any undo operation");
    Console.WriteLine("* Rename the undo file to cleanup.executor.txt - So whenever llmc runs and finds this file, it will execute all undo operation and then stop");

    Console.WriteLine("Commandline parameters:");
    Console.WriteLine("llmc.exe --help | -h | /? : Display this help message");
    Console.WriteLine("llmc.exe --noundo : Do not generate undo.executor.txt file");

    return;
}

// Commandline parameters.
bool noUndo = args.Contains("--noundo");

CommandLineParams commandLineParams = new(NoUndo: noUndo);

var geminiLlmConfiguration = new Configuration(
    Type: ConfigurationType.Llm,
    EnableGemini: true,
    ApiKeyEnvVar: "GEMINI_API_KEY",
    Url: "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={geminikey}");

var aoaiLlmConfiguration = new Configuration(
    Type: ConfigurationType.Llm,
    EnableAoai: true,
    ApiKeyEnvVar: "AOAI_API_KEY",
    Url: "https://icanazopenai.openai.azure.com/openai/deployments/gpt-4o/chat/completions?api-version=2024-10-21");

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

List<Configuration> configurations = [
    aoaiLlmConfiguration,
    geminiLlmConfiguration,
    aoaiEmbeddingConfiguration,
    geminiEmbeddingConfiguration
];

static void LogEnvironmentVariablesName(List<Configuration> configurations)
{
    foreach (var configuration in configurations)
    {
        Console.WriteLine($"ApiKeyEnvVar: {configuration.ApiKeyEnvVar}");
        Console.WriteLine($"GeminiUrlEnvVar: {configuration.GeminiUrlEnvVar}");
    }
}

LogEnvironmentVariablesName(configurations);

var llmConnector = new LlmConnector(configurations);
var promptDecorator = new PromptDecorator();
var promptExtractor = new PromptExtractor();
var executorFinder = new ExecutorFinder(llmConnector);
var executorInvoker = new ExecutorInvoker(llmConnector);
var fileRedactor = new FileRedactor(llmConnector, executorInvoker);
string projectPath = Directory.GetCurrentDirectory();
var projectLogic = new ProjectLogic(projectPath, commandLineParams, llmConnector, promptDecorator, promptExtractor, executorFinder, executorInvoker, fileRedactor);

var projectJson = projectLogic.ReadProjectJson(projectPath);

if (projectJson == null)
{
    projectPath = @"C:\B\L1\llmc\playground";
    projectJson = projectLogic.ReadProjectJson(@"C:\B\L1\llmc\playground");

    projectLogic = new ProjectLogic(projectPath, commandLineParams, llmConnector, promptDecorator, promptExtractor, executorFinder, executorInvoker, fileRedactor);
}

AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);

if (projectLogic.CheckForCleanup(projectPath))
{
    Console.WriteLine("Cleanup file found. Cleaning up...");
    projectLogic.Cleanup(projectPath);
    return;
}

var prompts = projectLogic.ReadPrompts();

// Handle each prompt separately.
foreach (var prompt in prompts)
{
    List<Prompt> unitPrompts = [prompt];

    projectLogic.ProcessPrebuildFeatures(unitPrompts);

    var llmResults = projectLogic.GetLlmResults(unitPrompts);

    var processFeatures = projectLogic.ProcessNonPrebuildFeatures(unitPrompts);

    string undoContent = string.Empty;

    if (!processFeatures.AnyFeatureProcessed)
    {
        undoContent = projectLogic.Process(llmResults);
    }

    // Post process.
    foreach (var unitPrompt in unitPrompts)
    {
        undoContent += projectLogic.PostProcess(unitPrompt);
    }

    if (!commandLineParams.NoUndo)
    {
        if (File.Exists(Path.Join(projectPath, "undo.executor.txt")))
        {
            undoContent = File.ReadAllText(
                Path.Join(projectPath, "undo.executor.txt")) +
                Environment.NewLine + undoContent;
        }

        File.WriteAllText(Path.Join(projectPath, "undo.executor.txt"), undoContent);
    }
}
