using llmc.Project;
using Newtonsoft.Json;
using System.Text;

namespace llmc.Connector;

internal class StdStreamEmbeddingClient : IEmbeddingClient
{
    public float[]? GetEmbedding(string input)
    {
        StringBuilder stringBuilder = new();

        string responseStart = $"<response>";
        string responseEnd = $"</response>";

        stringBuilder.AppendLine($"## Respond back text inside " +
            $"{responseStart} and {responseEnd} tags.");
        stringBuilder.AppendLine($"<prompt>");
        stringBuilder.AppendLine(input);
        stringBuilder.AppendLine("</prompt>");

        Console.WriteLine(stringBuilder.ToString());

        // Parse stdin buffer.
        StringBuilder response = new();

        while (!response.ToString().Trim().Contains(responseStart) ||
            !response.ToString().Trim().Contains(responseEnd))
        {
            int ch = Console.Read();

            if (ch >= 0) response.Append((char)ch);
        }

        string responseString = response.ToString();

        // Extract the response content.
        string content = responseString.Substring(
            responseString.IndexOf(responseStart) + responseStart.Length,
            responseString.IndexOf(responseEnd) - responseString.IndexOf(responseStart) - responseStart.Length);

        float[]? floats = JsonConvert.DeserializeObject<float[]>(content);

        return floats;
    }
}
