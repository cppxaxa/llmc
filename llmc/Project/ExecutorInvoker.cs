﻿
using llmc.Connector;
using llmc.Executor;
using System.Reflection;

namespace llmc.Project;

internal class ExecutorInvoker(
    LlmConnector connector)
{
    internal string Invoke(string parentPath, ExecutorFinderResult finderResult)
    {
        IExecutor? executor = Assembly.GetExecutingAssembly()
            .CreateInstance($"llmc.Executor.{finderResult.ClassName}") as IExecutor;

        if (executor == null)
        {
            Console.WriteLine($"Executor {finderResult.ClassName} not found");
        }
        else
        {
            // Inject dependencies.
            executor.Connector = connector;

            Console.WriteLine($"Executing {finderResult.ClassName} with param {finderResult.Param}");
            return executor.Execute(parentPath, finderResult.Param);
        }

        return string.Empty;
    }
}