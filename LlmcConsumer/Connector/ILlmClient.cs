namespace LlmcConsumer.Connector;

internal interface ILlmClient
{
     string Complete(string system, string prompt);
     string Complete(string prompt);
}
