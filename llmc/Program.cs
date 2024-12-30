
using llmc.Connector;
using llmc.Project;
using llmc.Storage;

if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h" || args[0] == "/?"))
{
    Console.WriteLine("* It looks for *.llmc.json as a project file. There should be only one such file in the directory.");
    Console.WriteLine("* It generates undo.executor.txt for any undo operation");
    Console.WriteLine("* Rename the undo file to cleanup.executor.txt - So whenever llmc runs and finds this file, it will execute all undo operation and then stop");

    Console.WriteLine("Commandline parameters:");
    Console.WriteLine("llmc.exe --help | -h | /? : Display this help message");
    Console.WriteLine("llmc.exe --noundo : Do not generate undo.executor.txt file");
    Console.WriteLine("llmc.exe --disableinmemorystorage : Disable in-memory storage flag");
    Console.WriteLine("llmc.exe --azurellm : Use Azure LLM");
    Console.WriteLine("llmc.exe --azureembedding : Use Azure Embedding");
    Console.WriteLine("llmc.exe --geminillm : Use Gemini LLM");
    Console.WriteLine("llmc.exe --geminiembedding : Use Gemini Embedding");

    return;
}

// Commandline parameters.
bool noUndo = args.Contains("--noundo");
bool disableInMemoryStorage = args.Contains("--disableinmemorystorage");
bool azureLlm = args.Contains("--azurellm");
bool azureEmbedding = args.Contains("--azureembedding");
bool geminiLlm = args.Contains("--geminillm");
bool geminiEmbedding = args.Contains("--geminiembedding");

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

// Production.
var stdStreamLlmConfiguration = new Configuration(Type: ConfigurationType.Llm, EnableStdStream: true);

// Production.
var stdStreamEmbeddingConfiguration = new Configuration(Type: ConfigurationType.Embedding, EnableStdStream: true);

List<Configuration> configurations = [
    stdStreamLlmConfiguration,
    stdStreamEmbeddingConfiguration,
    aoaiLlmConfiguration,
    aoaiEmbeddingConfiguration,
    geminiLlmConfiguration,
    geminiEmbeddingConfiguration,
];

if (azureLlm) configurations.Insert(0, aoaiLlmConfiguration);
if (azureEmbedding) configurations.Insert(0, aoaiEmbeddingConfiguration);
if (geminiLlm) configurations.Insert(0, geminiLlmConfiguration);
if (geminiEmbedding) configurations.Insert(0, geminiEmbeddingConfiguration);

void LogEnvironmentVariablesName()
{
    foreach (var configuration in configurations)
    {
        Console.WriteLine($"ApiKeyEnvVar: {configuration.ApiKeyEnvVar}");
        Console.WriteLine($"GeminiUrlEnvVar: {configuration.GeminiUrlEnvVar}");
    }

    if (azureLlm) Console.WriteLine("Switch: --azurellm");
    if (azureEmbedding) Console.WriteLine("Switch: --azureembedding");
    if (geminiLlm) Console.WriteLine("Switch: --geminillm");
    if (geminiEmbedding) Console.WriteLine("Switch: --geminiembedding");
}

LogEnvironmentVariablesName();

// Default behavior.
var storage = new SwitchableStorage(new(
    EnableInMemoryStorage: false));

string projectPath = Directory.GetCurrentDirectory();
ProjectModel? project = ProjectLogic.ReadProjectJson(storage, projectPath);

if (project == null)
{
    projectPath = @"C:\B\L1\llmc\playground";
    project = ProjectLogic.ReadProjectJson(storage, @"C:\B\L1\llmc\playground")!;
}

var llmConnector = new LlmConnector(configurations);
var promptDecorator = new PromptDecorator();
var promptExtractor = new PromptExtractor();
var executorFinder = new ExecutorFinder(llmConnector);
var executorInvoker = new ExecutorInvoker(project, storage, llmConnector);
var fileRedactor = new FileRedactor(llmConnector, executorInvoker);

var projectLogic = new ProjectLogic(projectPath, project, commandLineParams, storage, llmConnector, promptDecorator, promptExtractor, executorFinder, executorInvoker, fileRedactor);

// Validation.
projectLogic.Validate();

AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);

if (projectLogic.CheckForCleanup(projectPath))
{
    Console.WriteLine("Cleanup file found. Cleaning up...");
    projectLogic.Cleanup(projectPath);
    return;
}

var prompts = projectLogic.ReadPrompts(disableInMemoryStorage: disableInMemoryStorage);

// Handle each prompt separately.
foreach (var prompt in prompts)
{
    List<Prompt> unitPrompts = [prompt];

    projectLogic.ProcessPrebuildFeatures(unitPrompts);

    foreach (var unitPrompt in unitPrompts)
    {
        string promptString = promptDecorator.Decorate(unitPrompt.Text);
        projectLogic.PreProcess(prompt, promptString);
    }

    var processFeatures = projectLogic.ProcessNonPrebuildFeatures(unitPrompts);

    string undoContent = string.Empty;

    if (!processFeatures.AnyFeatureProcessed)
    {
        var llmResults = projectLogic.GetLlmResults(unitPrompts);

        undoContent = projectLogic.Process(llmResults);
    }

    // Post process.
    foreach (var unitPrompt in unitPrompts)
    {
        undoContent += projectLogic.PostProcess(unitPrompt);
    }

    if (!commandLineParams.NoUndo)
    {
        if (storage.Exists(Path.Join(projectPath, "undo.executor.txt")))
        {
            undoContent = storage.ReadAllText(
                Path.Join(projectPath, "undo.executor.txt")) +
                Environment.NewLine + undoContent;
        }

        storage.WriteAllText(Path.Join(projectPath, "undo.executor.txt"), undoContent);
    }
}

/* TODO:
 * [ ] C# library integrator 
 * [ ] Project json from stdin
 * [ ] Make prompts for presets
 */