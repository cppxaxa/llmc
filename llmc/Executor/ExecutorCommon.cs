using llmc.Connector;

namespace llmc.Executor;

internal abstract class ExecutorCommon : IExecutor
{
    public LlmConnector? Connector { get; set; }

    public abstract string Execute(string parentDirectory, string param);

}