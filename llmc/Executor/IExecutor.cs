using llmc.Connector;
using llmc.Project;

namespace llmc.Executor
{
    internal interface IExecutor
    {
        ProjectModel? Project { get; set; }
        LlmConnector? Connector { get; set; }

        // Returns command to undo.
        string Execute(string parentDirectory, string param);
    }
}