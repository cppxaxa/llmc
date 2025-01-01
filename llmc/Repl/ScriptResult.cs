namespace llmc.Repl;

public record ScriptResult(string Result, bool IsError, bool IsCancelled);