using llmc.Connector;
using llmc.Project;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Executor
{
    internal class MarkdownExtractor : ExecutorCommon
    {
        public override string Execute(string parentDirectory, string param)
        {
            StringBuilder undo = new();

            Dictionary<string, string> p = Common.ParseParam(param);
            string input = p["input"];
            string output = p["output"];
            string sectionsearch = p["sectionsearch"];
            string? removecodeannotation = p.ContainsKey("removecodeannotation") ? p["removecodeannotation"] : null;

            string inputFileFullPath = Path.Join(parentDirectory, input);
            string outputFileFullPath = Path.Join(parentDirectory, output);

            if (File.Exists(outputFileFullPath))
            {
                string newFilename = outputFileFullPath + ".bak-" + Guid.NewGuid();
                File.Move(outputFileFullPath, newFilename);

                undo.AppendLine($"MoveFile(from=\"{newFilename}\",to=\"{output}\")");
            }

            // TODO. Implement the logic to extract the markdown content.
            // Read the input file line by line.
            List<string> fileLines = File.ReadAllLines(inputFileFullPath).ToList();
            int sectionIndex = fileLines.FindIndex(line => line.Trim().StartsWith('#') &&
                line.Contains(sectionsearch));

            if (sectionIndex == -1)
            {
                Console.WriteLine($"MarkdownExtractor: Section '{sectionsearch}' not found in the input file '{inputFileFullPath}'.");

                return undo.ToString();
            }

            List<string> sectionLines = ExtractSection(fileLines, sectionIndex);
            List<string> codesection = [];

            if (removecodeannotation != null)
            {
                for (int i = 0; i < sectionLines.Count; i++)
                {
                    string line = sectionLines[i];

                    if (line.Trim().StartsWith("```"))
                    {
                        for (int j = i + 1; j < sectionLines.Count; j++)
                        {
                            if (sectionLines[j].Trim().StartsWith("```"))
                            {
                                i = j;
                                break;
                            }
                     
                            codesection.Add(sectionLines[j]);
                        }
                    }

                    if (codesection.Count > 0) break;
                }
            }

            File.WriteAllLines(outputFileFullPath, codesection);
            Console.WriteLine($"MarkdownExtractor: Extracted the code section '{sectionsearch}' from '{inputFileFullPath}' to '{outputFileFullPath}'");

            undo.AppendLine($"DeleteFile(filename=\"{outputFileFullPath}\")");

            return undo.ToString();
        }

        private static List<string> ExtractSection(List<string> fileLines, int sectionIndex)
        {
            List<string> sectionLines = [];

            for (int i = sectionIndex; i < fileLines.Count; i++)
            {
                string line = fileLines[i];

                if (line.Trim().StartsWith('#') && i != sectionIndex)
                {
                    break;
                }

                sectionLines.Add(line);
            }

            return sectionLines;
        }
    }
}
