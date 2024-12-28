using llmc.Connector;
using llmc.Project;
using llmc.Storage;

namespace llmc.Executor
{
    internal interface IExecutor
    {
        ProjectModel? Project { get; set; }
        LlmConnector? Connector { get; set; }
        IStorage? Storage { get; set; }

        // Returns command to undo.
        string Execute(string parentDirectory, string param);
    }
}