using llmc.Connector;
using llmc.Project;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Executor;

internal class RedactFile : ExecutorCommon
{
    public override string Execute(string parentDirectory, string param)
    {
        StringBuilder undo = new();

        Dictionary<string, string> p = Common.ParseParam(param);
        string filename = p["filename"];
        List<string> lines = JsonConvert.DeserializeObject<List<string>>(p["lines"])!;

        Console.WriteLine($"RedactFile:Redaction started on file {filename} in " +
            $"{lines.Count} parts.");

        if (File.Exists(Path.Join(parentDirectory, filename)))
        {
            string[] fileLines = File.ReadAllLines(Path.Join(parentDirectory, filename));

            int c = 1;

            foreach (string line in lines)
            {
                string[] lineSplit = line.Split('-');
                int inclusiveStart = int.Parse(lineSplit[0]);
                int inclusiveEnd = int.Parse(lineSplit[1]);

                // Extract the content.
                string[] strings = fileLines[(inclusiveStart-1)..(inclusiveEnd)];

                // Redact the content.
                for (int i = inclusiveStart; i <= inclusiveEnd; i++)
                {
                    fileLines[i - 1] = $"[REDACTED ... {c}]";
                }

                // Form redacted content filename.
                string redactedFilename = filename + $".redacted-{c}";

                // Write redacted content to a new file.
                File.WriteAllLines(
                    Path.Join(parentDirectory, redactedFilename),
                    strings);

                c += 1;
            }

            // Reduce the duplicate redacted lines into one.
            int idx = 0;

            for (int i = 1; i < fileLines.Length; i++)
            {
                if (fileLines[idx].Contains("[REDACTED ...") &&
                    fileLines[i] == fileLines[idx])
                {
                    continue;
                }
                else
                {
                    idx += 1;
                    fileLines[idx] = fileLines[i];
                }
            }

            File.WriteAllText(
                Path.Join(parentDirectory, filename),
                string.Join(Environment.NewLine, fileLines[0..(idx+1)]));

            undo.AppendLine($"UndoRedactedFile(filename=\"{filename}\")");
        }
        else
        {
            Console.WriteLine($"RedactFile:File {filename} does not exist.");
        }

        return undo.ToString();
    }
}
