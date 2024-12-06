using llmc.Connector;
using llmc.Project;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Executor;

internal class UndoRedactedFile : ExecutorCommon
{
    public override string Execute(string parentDirectory, string param)
    {
        StringBuilder undo = new();

        Dictionary<string, string> p = Common.ParseParam(param);
        string filename = p["filename"];

        Console.WriteLine($"UndoRedactedFile:Redaction started on file {filename}.");

        if (File.Exists(Path.Join(parentDirectory, filename)))
        {
            string directory = Path.GetDirectoryName(Path.Join(parentDirectory, filename))!;
            string finalFilename = Path.GetFileName(filename);

            // List all the redacted files.
            string[] redactedFiles = Directory.GetFiles(directory, $"{finalFilename}.redacted-*");

            Console.WriteLine($"UndoRedactedFile:Found {redactedFiles.Length} redacted files.");

            string content = File.ReadAllText(Path.Join(parentDirectory, filename));

            foreach (var redactedFile in redactedFiles)
            {
                string number = redactedFile.Split(".redacted-")[1];
                string placeholder = $"[REDACTED ... {number}]";

                string redactedContent = string.Join(
                    Environment.NewLine, File.ReadAllLines(redactedFile));

                content = content.Replace(placeholder, redactedContent);
            }

            File.WriteAllText(
                Path.Join(parentDirectory, filename), content);

            // Delete all the redacted files.
            foreach (var redactedFile in redactedFiles)
            {
                File.Delete(redactedFile);
            }
        }
        else
        {
            Console.WriteLine($"UndoRedactedFile:File {filename} does not exist.");
        }

        return undo.ToString();
    }
}
