using llmc.Connector;
using llmc.Project;
using llmc.Storage;

namespace llmc.Executor;

internal abstract class ExecutorCommon : IExecutor
{
    public ProjectModel? Project { get; set; }
    public LlmConnector? Connector { get; set; }
    public IStorage? Storage { get; set; }

    public abstract string Execute(string parentDirectory, string param);
}