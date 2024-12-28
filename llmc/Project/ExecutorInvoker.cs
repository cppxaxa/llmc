
using llmc.Connector;
using llmc.Executor;
using System.Reflection;

namespace llmc.Project;

internal class ExecutorInvoker(
    ProjectModel project,
    LlmConnector connector)
{
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
            executor.Connector = connector;

            Console.WriteLine($"Executing {finderResult.ClassName} with param {finderResult.Param}");
            return executor.Execute(parentPath, finderResult.Param);
        }

        return string.Empty;
    }
}