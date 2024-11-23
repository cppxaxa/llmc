namespace llmc.Executor
{
    internal interface IExecutor
    {
        // Returns command to undo.
        string Execute(string parentDirectory, string param);
    }
}