using llmc.Project;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace llmc.Features;

internal class HybridSearch : FeatureCommon
{
    public override bool AsPrebuild => true;

    // Collection to store vectors and their associated strings
    private List<(float[] Vector, string Label)> _vectorCollection = [];
    private List<(string, int)> _searchTextCollection = [];  

    public override void Execute(string parentDirectory, string param)
    {
        Console.WriteLine("Executing feature HybridSearch: " + param);

        StringBuilder undo = new();

        EnsureThat.EnsureArg.IsNotNull(Storage, nameof(Storage));
        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));
        EnsureThat.EnsureArg.IsNotNull(Prompt, nameof(Prompt));
        EnsureThat.EnsureArg.IsNotNull(ExecutorInvoker, nameof(ExecutorInvoker));

        Dictionary<string, string> p = Common.ParseParam(param);
        string source = p["source"];
        string file = p["file"];
        string nresults = p["nresults"];
        string slidingwordsize = p["slidingwordsize"];
        string slidingwordoverlap = p["slidingwordoverlap"];
        string? searchJsonKey = p.ContainsKey("searchjsonkey") ? p["searchjsonkey"] : null;

        int page = int.Parse(nresults);
        string outFilePath = Path.Combine(parentDirectory, file);

        string promptText = Prompt.Text;
        string res;

        // Clean the prompt text to avoid further consumption.
        Prompt.Text = string.Empty;

        // Index for the collection.
        int cidx = 0;

        // Load the vectors from the source.
        foreach (var f in Storage.GetFiles(Path.Combine(parentDirectory, source)))
        {
            foreach (var line in Storage.ReadAllLines(f))
            {
                var vectorDocument = JsonConvert.DeserializeObject<VectorDocument>(line)
                    ?? throw new Exception("Error in parsing jsonl file");

                AddVector(vectorDocument.embedding, vectorDocument.jsonLine);

                AddSearchText(searchJsonKey, cidx, vectorDocument.jsonLine);

                cidx += 1;
            }
        }

        // Search.
        HashSet<string> stopWords = GetStopWords();
        HashSet<string> tokens = GetSearchTextTokens(promptText, stopWords);
        string simplerQuery = GetSimplerQueryWithLlm(promptText, tokens);

        string simplerQueryInPlainText = Common.RemoveCodeAnnotations(simplerQuery);

        List<string> brokenPromptList = BreakPrompts(simplerQueryInPlainText, slidingwordsize, slidingwordoverlap);

        HashSet<string> searchResult = new();

        // Vector search.
        foreach (var brokenPrompt in brokenPromptList)
        {
            float[] vector = Connector.GetEmbedding(brokenPrompt)
                ?? throw new Exception("Error in getting embedding for search");
            var result = SearchVector(vector, page);

            foreach (var (Vector, Label, Similarity) in result)
            {
                searchResult.Add(Label);
            }
        }

        // Text search.
        foreach (var (searchText, idx) in _searchTextCollection)
        {
            foreach (var token in tokens)
            {
                if (Regex.IsMatch(searchText, $@"\b{Regex.Escape(token)}\b", RegexOptions.IgnoreCase))
                {
                    searchResult.Add(_vectorCollection[idx].Label);
                    break;
                }
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
    }

    private string GetSimplerQueryWithLlm(string promptText, HashSet<string> tokens)
    {
        EnsureThat.EnsureArg.IsNotNull(Connector, nameof(Connector));

        return Connector.Complete($"System: Respond within ```plaintext markdown code annotations.\n" +
            $"User: Can you simplify the query for search system, yet keeping the query understandable, and preserve the keywords: " +
            $"{JsonConvert.SerializeObject(tokens)} from the query '{promptText.Trim()}'. " +
            $"Pass on necessary verbs like and, or, commas that helps separate keywords in query into the new query.\n" +
            $"Assistant: The new query in code annotations is below.\n");
    }

    private static HashSet<string> GetSearchTextTokens(string promptText, HashSet<string> stopWords)
    {

        // Split, clean, and filter tokens
        var tokens = new HashSet<string>(
            promptText
                .Trim()
                .Split(' ')
                .Select(token => Regex.Replace(token, @"[^\w]", "")) // Remove non-word characters
                .Where(token => !string.IsNullOrEmpty(token))       // Remove empty strings
                .Where(token =>
                    !stopWords.Contains(token) ||                  // Keep words not in stop words list
                    !Common.IsLowercaseOrPascalCase(token))     // Keep words not matching case rules
        );
        return tokens;
    }

    private static HashSet<string> GetStopWords()
    {

        // Text search.
        // Define an expanded list of stop words
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "ad", "all", "an", "and", "any", "are", "as", "ask", "at", "be", "but", "by",
            "call", "can", "come", "could", "did", "do", "does", "down", "each", "every",
            "feel", "few", "find", "for", "from", "get", "give", "go", "help", "had", "has", "have",
            "he", "her", "how", "i", "if", "in", "into", "is", "it", "its", "know", "leave",
            "let", "let's", "look", "make", "many", "may", "me", "might", "more", "most",
            "much", "must", "my", "need", "no", "not", "of", "on", "or", "other", "our",
            "ours", "out", "say", "see", "shall", "she", "should", "show", "so", "some",
            "such", "take", "tell", "than", "that", "the", "their", "them", "then", "there",
            "these", "they", "think", "this", "to", "too", "try", "up", "use", "very",
            "want", "was", "we", "what", "when", "where", "which", "who", "why", "will",
            "with", "work", "would", "yes", "you", "your"
        };
    }

    private void AddSearchText(string? searchJsonKey, int c, string jsonLine)
    {
        if (searchJsonKey != null)
        {
            JObject jObject = JsonConvert.DeserializeObject<JObject>(jsonLine)
                ?? throw new Exception("Error in parsing jsonl file");

            string? searchText = jObject[searchJsonKey]?.ToString();

            if (searchText != null)
                _searchTextCollection.Add((searchText, c));
        }
    }

    private List<string> BreakPrompts(
        string promptText, string slidingwordsizeStr, string slidingwordoverlapStr)
    {
        int slidingWordSize = int.Parse(slidingwordsizeStr);
        int slidingWordOverlap = int.Parse(slidingwordoverlapStr);

        // Validations.
        if (slidingWordOverlap < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slidingwordoverlapStr), "Must be greater than 0");
        }

        if (slidingWordSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(slidingwordsizeStr), "Must be greater than 0");
        }

        if (slidingWordOverlap > slidingWordSize)
        {
            throw new ArgumentException("Must be less than slidingWordSize", nameof(slidingwordoverlapStr));
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
