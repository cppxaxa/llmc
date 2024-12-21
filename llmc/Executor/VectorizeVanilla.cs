using llmc.Project;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace llmc.Executor;

internal class VectorizeVanilla : ExecutorCommon
{
    public override string Execute(string parentDirectory, string param)
    {
        StringBuilder undo = new();

        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));

        Dictionary<string, string> p = Common.ParseParam(param);
        string jsonlfolder = p["jsonlfolder"];
        string outFolder = p["out"];
        string fullOutFolder = Path.Combine(parentDirectory, outFolder);

        if (!Directory.Exists(fullOutFolder))
        {
            Directory.CreateDirectory(fullOutFolder);
        }

        // Read all the jsonl files.
        var files = Directory.GetFiles(Path.Combine(parentDirectory, jsonlfolder), "*.jsonl");

        List<VectorDocument> vectorDocuments = [];
        int flushCounter = 0;

        foreach (var f in files)
        {
            var lines = File.ReadAllLines(f);

            foreach (var l in lines)
            {
                List<string> keys = [];
                List<string> values = [];

                try
                {
                    var jObject = JsonConvert.DeserializeObject<JObject>(l)
                        ?? throw new Exception("Error in parsing jsonl file");

                    foreach (var item in jObject)
                    {
                        keys.Add(item.Key);
                        values.Add(item.Value?.ToString() ?? string.Empty);
                    }
                }
                catch
                {
                    Console.WriteLine($"Error in parsing jsonl file {f}");
                }

                // Process keys.
                StringBuilder toVectorize = new();

                for (int i = 0; i < keys.Count; i++)
                {
                    if (!keys[i].StartsWith('_'))
                    {
                        toVectorize.AppendLine($"{keys[i]} = {values[i]}\n");
                    }
                }

                float[]? embedding = Connector.GetEmbedding(toVectorize.ToString())
                    ?? throw new Exception("Error in getting embedding");

                // Out.
                vectorDocuments.Add(new VectorDocument(l, embedding));

                // Write the embedding to a file.
                if (vectorDocuments.Count > 50)
                {
                    AppendVectors(fullOutFolder, vectorDocuments, flushCounter);

                    vectorDocuments.Clear();
                    flushCounter += 1;
                }
            }
        }

        // Write the remaining embeddings.
        if (vectorDocuments.Count > 0)
        {
            AppendVectors(fullOutFolder, vectorDocuments, flushCounter);
        }

        return undo.ToString();
    }

    private static void AppendVectors(string fullOutFolder, List<VectorDocument> vectorDocuments, int flushCounter)
    {
        string filename = Path.Combine(fullOutFolder, $"embedding-{flushCounter}.jsonl");
        List<string> outLines = vectorDocuments.Select(JsonConvert.SerializeObject).ToList();
        File.AppendAllLines(filename, outLines);
    }

    internal record VectorDocument(string jsonLine, float[] embedding);
}
