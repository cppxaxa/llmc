using llmc.Connector;

namespace llmc.Executor
{
    internal interface IExecutor
    {
        LlmConnector? Connector { get; set; }

        // Returns command to undo.
        string Execute(string parentDirectory, string param);
    }
}