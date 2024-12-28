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

        EnsureThat.EnsureArg.IsNotNull(Storage);
        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));

        Dictionary<string, string> p = Common.ParseParam(param);
        string jsonlfolder = p["jsonlfolder"];
        string outFolder = p["out"];
        string searchJsonKey = p["searchjsonkey"];

        string fullOutFolder = Path.Combine(parentDirectory, outFolder);

        if (!Storage.Exists(fullOutFolder))
        {
            Storage.CreateDirectory(fullOutFolder);
        }

        // Read all the jsonl files.
        var files = Storage.GetFiles(Path.Combine(parentDirectory, jsonlfolder), "*.jsonl");

        List<VectorDocument> vectorDocuments = [];
        int flushCounter = 0;

        foreach (var f in files)
        {
            var lines = Storage.ReadAllLines(f);

            foreach (var l in lines)
            {
                JObject jObject = JsonConvert.DeserializeObject<JObject>(l)
                    ?? throw new Exception("Error in parsing jsonl file");

                string? toVectorize = jObject[searchJsonKey]?.ToString();

                if (toVectorize == null)
                {
                    Console.WriteLine($"Error in parsing jsonl file {f}");
                    continue;
                }

                float[]? embedding = Connector.GetEmbedding(toVectorize.ToString())
                    ?? throw new Exception("Error in getting embedding");

                // Out.
                vectorDocuments.Add(new VectorDocument(l, embedding));

                // Write the embedding to a Storage.
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

    private void AppendVectors(string fullOutFolder, List<VectorDocument> vectorDocuments, int flushCounter)
    {
        EnsureThat.EnsureArg.IsNotNull(Storage);
        
        string filename = Path.Combine(fullOutFolder, $"embedding-{flushCounter}.jsonl");
        List<string> outLines = vectorDocuments.Select(JsonConvert.SerializeObject).ToList();
        Storage.AppendAllLines(filename, outLines);
    }

    internal record VectorDocument(string jsonLine, float[] embedding);
}
