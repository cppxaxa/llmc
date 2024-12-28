using HandlebarsDotNet;
using llmc.Connector;
using llmc.Project;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace llmc.Executor
{
    internal class ApplyMacros : ExecutorCommon
    {
        /// <summary>
        /// Applies the project macros over handlebar syntax.
        /// </summary>
        public override string Execute(string parentDirectory, string param)
        {
            EnsureThat.EnsureArg.IsNotNull(Project, nameof(Project));

            StringBuilder undo = new();

            Dictionary<string, string> p = Common.ParseParam(param);
            string folder = p["folder"];
            string wildcard = p["wildcard"];

            string inputFolder = Path.Join(parentDirectory, folder);

            foreach (var filename in Directory.EnumerateFiles(inputFolder, wildcard))
            {
                string originalContent = string.Empty;

                if (File.Exists(filename))
                {
                    originalContent = File.ReadAllText(filename);

                    originalContent = Common.ApplyProjectMacros(Project, originalContent);

                    string newFilename = filename + ".bak-" + Guid.NewGuid();
                    File.Move(filename, newFilename);

                    undo.AppendLine($"MoveFile(from=\"{newFilename}\",to=\"{filename}\")");
                }

                string content = GetRenderedContent(originalContent, inputFolder);

                File.WriteAllText(filename, content);

                undo.AppendLine($"DeleteFile(filename=\"{filename}\")");
            }

            return undo.ToString();
        }

        private static string GetRenderedContent(string templateContent, string templateFolder)
        {
            var template = Handlebars.Compile(templateContent);

            var data = new Dictionary<string, string>();

            // Populate each file content.
            foreach (var filename in Directory.EnumerateFiles(templateFolder))
            {
                string content = File.ReadAllText(filename);

                // Trim the content.
                int limit = 15000;

                if (content.Length >= limit)
                {
                    content = content.Substring(0, limit);
                    Console.WriteLine($"{nameof(ApplyMacros)}: Trimmed content of {filename} to {limit} characters.");
                }

                string key = Path.GetFileName(filename).Replace('.', '_');

                data[key] = content;
            }

            var renderedOutput = template(data);

            return renderedOutput;
        }
    }
}
