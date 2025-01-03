
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
    Console.WriteLine("llmc.exe --parentprocessid <id> : Parent process id");
    Console.WriteLine("llmc.exe --verbose : Enable verbose logging");
    Console.WriteLine("llmc.exe --noundo : Do not generate undo.executor.txt file");
    Console.WriteLine("llmc.exe --disableinmemorystorage : Disable in-memory storage flag");
    Console.WriteLine("llmc.exe --azurellm : Use Azure LLM");
    Console.WriteLine("llmc.exe --azureembedding : Use Azure Embedding");
    Console.WriteLine("llmc.exe --geminillm : Use Gemini LLM");
    Console.WriteLine("llmc.exe --geminiembedding : Use Gemini Embedding");
    Console.WriteLine("llmc.exe --stdinprojectjson : Read project json from stdin");
    Console.WriteLine("llmc.exe --projectpath <path> : Path to the project file. Default is current directory");

    return;
}

// Commandline parameters.
bool verbose = args.Contains("--verbose");
int parentProcessIdIndex = args.Select((e, i) => (e, i))
    .Where(e => e.e == "--parentprocessid").FirstOrDefault(("", -1)).Item2;
int parentProcessId = parentProcessIdIndex != -1 && args.Length > parentProcessIdIndex + 1
    ? int.Parse(args[parentProcessIdIndex + 1]) : -1;
bool noUndo = args.Contains("--noundo");
bool disableInMemoryStorage = args.Contains("--disableinmemorystorage");
bool azureLlm = args.Contains("--azurellm");
bool azureEmbedding = args.Contains("--azureembedding");
bool geminiLlm = args.Contains("--geminillm");
bool geminiEmbedding = args.Contains("--geminiembedding");
bool stdinProjectJson = args.Contains("--stdinprojectjson");
int projectPathIndex = args.Select((e, i) => (e, i))
    .Where(e => e.e == "--projectpath").FirstOrDefault(("", -1)).Item2;
string projectPath = projectPathIndex != -1 && args.Length > projectPathIndex + 1
    ? args[projectPathIndex + 1] : Directory.GetCurrentDirectory();

// Honor parent process id.
Thread monitoringThread = new(() => Common.MonitorProcess(parentProcessId));
monitoringThread.IsBackground = true; // Mark as background thread
monitoringThread.Start();

CommandLineParams commandLineParams = new(VerboseLogging: verbose, NoUndo: noUndo);

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

void LogCommandLineParams()
{
    foreach (var configuration in configurations)
    {
        Console.WriteLine($"ApiKeyEnvVar: {configuration.ApiKeyEnvVar}");
        Console.WriteLine($"GeminiUrlEnvVar: {configuration.GeminiUrlEnvVar}");
    }

    if (verbose) Console.WriteLine("Switch: --verbose");
    if (parentProcessIdIndex != -1) Console.WriteLine($"Parameter: --parentprocessid {parentProcessId}");
    if (noUndo) Console.WriteLine("Switch: --noundo");
    if (disableInMemoryStorage) Console.WriteLine("Switch: --disableinmemorystorage");
    if (azureLlm) Console.WriteLine("Switch: --azurellm");
    if (azureEmbedding) Console.WriteLine("Switch: --azureembedding");
    if (geminiLlm) Console.WriteLine("Switch: --geminillm");
    if (geminiEmbedding) Console.WriteLine("Switch: --geminiembedding");
    if (stdinProjectJson) Console.WriteLine("Switch: --stdinprojectjson");
    if (projectPathIndex != -1) Console.WriteLine($"Parameter: --projectpath {projectPath}");
}

LogCommandLineParams();

// Default behavior.
var storage = new SwitchableStorage(new(EnableInMemoryStorage: false));
ProjectModel? project = null!;

// Read project json.
if (stdinProjectJson)
{
    Console.WriteLine("Waiting for project json from stdin ...");
    project = ProjectLogic.ReadProjectJsonFromStdin();
}
else
{
    project = ProjectLogic.ReadProjectJson(storage, projectPath);
}

// Validate project.
if (project == null)
{
    Console.WriteLine("Unable to figure out the project. Exiting...");
    return;
}

var llmConnector = new LlmConnector(configurations);
var promptDecorator = new PromptDecorator();
var promptExtractor = new PromptExtractor();
var executorFinder = new ExecutorFinder(llmConnector);
var executorInvoker = new ExecutorInvoker(project, storage, llmConnector);
var fileRedactor = new FileRedactor(llmConnector, executorInvoker);

var projectLogic = new ProjectLogic(
    projectPath, project, commandLineParams, storage,
    llmConnector, promptDecorator, promptExtractor,
    executorFinder, executorInvoker, fileRedactor);

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
 * [x] ConsoleWriteline tag support
 * [ ] Retry support, based on acceptance C# logic
 * [ ] C# library integrator with LLM callbacks, projectJson input, and consoleoutput
 * 
 * [ ] Graph representation of whole project
 * [ ] Curl command template support with C# script parser
 * [ ] Match word search in searchtext
 */