
using llmc.Connector;
using llmc.Project;

if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h" || args[0] == "/?"))
{
    Console.WriteLine("* It looks for *.llmc.json as a project file. There should be only one such file in the directory.");
    Console.WriteLine("* It generates undo.executor.txt for any undo operation");
    Console.WriteLine("* Rename the undo file to cleanup.executor.txt - So whenever llmc runs and finds this file, it will execute all undo operation and then stop");
}

var llmConfiguration = new Configuration(
    EnabledGemini: true,
    GeminiKeyEnvVar: "GEMINI_API_KEY",
    GeminiUrlEnvVar: string.Empty,
    GeminiUrl: "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={geminikey}");

var llmConnector = new LlmConnector(llmConfiguration);
var promptDecorator = new PromptDecorator();
var promptExtractor = new PromptExtractor();
var executorFinder = new ExecutorFinder(llmConnector);
var executorInvoker = new ExecutorInvoker(llmConnector);
var fileRedactor = new FileRedactor(llmConnector, executorInvoker);
string projectPath = Directory.GetCurrentDirectory();
var projectLogic = new ProjectLogic(projectPath, llmConnector, promptDecorator, promptExtractor, executorFinder, executorInvoker, fileRedactor);

var projectJson = projectLogic.ReadProjectJson();

if (projectJson == null)
{
    projectPath = @"C:\B\L1\llmc\playground";
}

AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);

projectLogic = new ProjectLogic(projectPath, llmConnector, promptDecorator, promptExtractor, executorFinder, executorInvoker, fileRedactor);

if (projectLogic.CheckForCleanup(projectPath))
{
    Console.WriteLine("Cleanup file found. Cleaning up...");
    projectLogic.Cleanup(projectPath);
    return;
}

var prompts = projectLogic.ReadPrompts();

projectLogic.ProcessPrebuildFeatures(prompts);

var llmResults = projectLogic.GetLlmResults(prompts);

var processFeatures = projectLogic.ProcessNonPrebuildFeatures(prompts);

string undoContent = string.Empty;

if (!processFeatures.AnyFeatureProcessed)
{
    undoContent = projectLogic.Process(llmResults);
}

if (File.Exists(Path.Join(projectPath, "undo.executor.txt")))
{
    undoContent = File.ReadAllText(
        Path.Join(projectPath, "undo.executor.txt")) +
        Environment.NewLine + undoContent;
}

File.WriteAllText(Path.Join(projectPath, "undo.executor.txt"), undoContent);
