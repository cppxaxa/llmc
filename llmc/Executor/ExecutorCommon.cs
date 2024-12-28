using llmc.Connector;
using llmc.Project;

namespace llmc.Executor;

internal abstract class ExecutorCommon : IExecutor
{
    public ProjectModel? Project { get; set; }
    public LlmConnector? Connector { get; set; }

    public abstract string Execute(string parentDirectory, string param);

}