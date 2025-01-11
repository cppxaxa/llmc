using llmc.Project;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;

namespace llmc.Features;

internal class VectorizedVanillaSearch : FeatureCommon
{
    public override bool AsPrebuild => true;

    public override FeatureResult Execute(string parentDirectory, string param)
    {
        Console.WriteLine("Executing feature VectorizedVanillaSearch: " + param);

        StringBuilder undo = new();

        EnsureThat.EnsureArg.IsNotNull(Storage);
        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));
        EnsureThat.EnsureArg.IsNotNull(Prompt, nameof(Prompt));
        EnsureThat.EnsureArg.IsNotNull(ExecutorInvoker, nameof(ExecutorInvoker));

        Dictionary<string, string> p = Common.ParseParam(param);
        string source = p["source"];
        string file = p["file"];
        string nresults = p["nresults"];
        string slidingwordsize = p["slidingwordsize"];
        string slidingwordoverlap = p["slidingwordoverlap"];

        int page = int.Parse(nresults);
        string outFilePath = Path.Combine(parentDirectory, file);

        string promptText = Prompt.Text;
        string res;

        // Clean the prompt text to avoid further consumption.
        Prompt.Text = string.Empty;

        // Load the vectors from the source.
        foreach (var f in Storage.GetFiles(Path.Combine(parentDirectory, source)))
        {
            foreach (var line in Storage.ReadAllLines(f))
            {
                var vectorDocument = JsonConvert.DeserializeObject<VectorDocument>(line)
                    ?? throw new Exception("Error in parsing jsonl file");

                AddVector(vectorDocument.embedding, vectorDocument.jsonLine);
            }
        }

        // Search.
        List<string> brokenPromptList = BreakPrompts(promptText, slidingwordsize, slidingwordoverlap);

        HashSet<string> searchResult = new();

        foreach (var brokenPrompt in brokenPromptList)
        {
            float[] vector = Connector.GetEmbedding(brokenPrompt)
                ?? throw new Exception("Error in getting embedding for search");
            var result = SearchVector(vector, page);

            foreach (var r in result)
            {
                searchResult.Add(r.Label);
            }
        }

        if (Storage.Exists(outFilePath))
        {
            string newFile = $"{file}.{Guid.NewGuid()}.bak";

            res = ExecutorInvoker.Clone().ChangeStorage(Storage).Invoke(
                parentDirectory,
                new ExecutorFinderResult("MoveFile", $"from=\"{file}\",to=\"{newFile}\""));

            undo.AppendLine(res);
        }

        // Save the search result.
        string searchResultParentDirectory = Path.GetDirectoryName(outFilePath)
            ?? throw new Exception("Error in getting parent directory");

        List<ExecutorFinderResult> appendUndoList = [];

        if (!Storage.Exists(searchResultParentDirectory))
        {
            Storage.CreateDirectory(searchResultParentDirectory);

            appendUndoList.Add(new ExecutorFinderResult(
                "AppendUndo", $"fn=\"DeleteFolder\",folder=\"{searchResultParentDirectory}\""));
        }

        Storage.WriteAllText(outFilePath, string.Join(Environment.NewLine, searchResult));

        appendUndoList.Add(new ExecutorFinderResult(
            "AppendUndo", $"fn=\"DeleteFile\",filename=\"{file}\""));

        // Add all undo.
        foreach (var appendUndoAction in appendUndoList)
        {
            ExecutorInvoker.Clone().ChangeStorage(Storage).Invoke(
                parentDirectory, appendUndoAction);
        }

        ExecutorInvoker.Clone().ChangeStorage(Storage).Invoke(
            parentDirectory, new ExecutorFinderResult(
                "AppendUndo", $"dump={JsonConvert.SerializeObject(undo.ToString())}"));

        return new FeatureResult();
    }

    private List<string> BreakPrompts(
        string promptText, string slidingwordsizeStr, string slidingwordoverlapStr)
    {
        int slidingWordSize = int.Parse(slidingwordsizeStr);
        int slidingWordOverlap = int.Parse(slidingwordoverlapStr);

        // Validations.
        if (slidingWordOverlap < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slidingWordOverlap), "Must be greater than 0");
        }

        if (slidingWordSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(slidingWordSize), "Must be greater than 0");
        }

        if (slidingWordOverlap > slidingWordSize)
        {
            throw new ArgumentException(nameof(slidingWordOverlap), "Must be less than slidingWordSize");
        }

        string[] tokens = promptText.Split(' ');
        int i = 0;

        List<string> result = [];

        while (i < tokens.Length)
        {
            StringBuilder query = new();

            int nWordsLimit = Math.Min(slidingWordSize, tokens.Length - i);

            for (int j = 0; j < nWordsLimit; j++)
            {
                query.Append(tokens[i + j]);
                query.Append(" ");
            }
            
            result.Add(query.ToString());

            if (i + nWordsLimit >= tokens.Length)
                break;

            // Increment.
            i = i + nWordsLimit - slidingWordOverlap;
        }

        return result;
    }

    // Collection to store vectors and their associated strings
    private List<(float[] Vector, string Label)> _vectorCollection = [];

    // Method to add a vector and its label to the collection
    public void AddVector(float[] vector, string label)
    {
        _vectorCollection.Add((vector, label));
    }

    // Method to compute cosine similarity between two vectors
    private static float CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            throw new ArgumentException("Vectors must have the same length.");

        float dotProduct = 0;
        float magnitudeA = 0;
        float magnitudeB = 0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        magnitudeA = (float)Math.Sqrt(magnitudeA);
        magnitudeB = (float)Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0; // Avoid division by zero

        return dotProduct / (magnitudeA * magnitudeB);
    }

    // Method to search for the most similar vector using cosine similarity (parallelized)
    public List<(float[] Vector, string Label, float Similarity)> SearchVector(
        float[] queryVector, int page)
    {
        if (!_vectorCollection.Any())
            throw new InvalidOperationException("No vectors in the collection to search.");

        var bestMatch = new ConcurrentBag<(float[] Vector, string Label, float Similarity)>();
        float maxSimilarity = float.MinValue;

        Parallel.ForEach(_vectorCollection, (entry) =>
        {
            var (vector, label) = entry;
            float similarity = CosineSimilarity(queryVector, vector);

            if (similarity > maxSimilarity)
            {
                maxSimilarity = similarity;
                bestMatch.Add((vector, label, similarity));
            }
        });

        // Get the best match from the concurrent bag
        var finalMatch = bestMatch.OrderByDescending(x => x.Similarity).Take(page).ToList();

        return finalMatch;
    }

    internal record VectorDocument(string jsonLine, float[] embedding);
}
