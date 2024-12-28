
using llmc.Connector;
using llmc.Executor;
using llmc.Storage;
using System.Reflection;

namespace llmc.Project;

internal class ExecutorInvoker(
    ProjectModel project,
    IStorage _storage,
    LlmConnector connector)
{
    private IStorage storage = _storage;

    public ExecutorInvoker Clone()
    {
        return new ExecutorInvoker(
            project: project, _storage: storage, connector: connector);
    }

    public ExecutorInvoker ChangeStorage(IStorage _storage)
    {
        storage = _storage;
        return this;
    }

    internal string Invoke(string parentPath, ExecutorFinderResult finderResult)
    {
        if (Assembly.GetExecutingAssembly()
            .CreateInstance($"llmc.Executor.{finderResult.ClassName}") is not IExecutor executor)
        {
            Console.WriteLine($"Executor {finderResult.ClassName} not found");
        }
        else
        {
            // Inject dependencies.
            executor.Project = project;
            executor.Storage = storage;
            executor.Connector = connector;

            Console.WriteLine($"Executing {finderResult.ClassName} with param {finderResult.Param}");
            return executor.Execute(parentPath, finderResult.Param);
        }

        return string.Empty;
    }
}