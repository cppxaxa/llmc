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
            EnsureThat.EnsureArg.IsNotNull(Storage);
            EnsureThat.EnsureArg.IsNotNull(Project, nameof(Project));

            StringBuilder undo = new();

            Dictionary<string, string> p = Common.ParseParam(param);
            string folder = p["folder"];
            string wildcard = p["wildcard"];

            string inputFolder = Path.Join(parentDirectory, folder);

            foreach (var filename in Storage.EnumerateFiles(inputFolder, wildcard))
            {
                string originalContent = string.Empty;

                if (Storage.Exists(filename))
                {
                    originalContent = Storage.ReadAllText(filename);

                    originalContent = Common.ApplyProjectMacros(Project, originalContent);

                    string newFilename = filename + ".bak-" + Guid.NewGuid();
                    Storage.Move(filename, newFilename);

                    undo.AppendLine($"MoveFile(from=\"{newFilename}\",to=\"{filename}\")");
                }

                string content = GetRenderedContent(originalContent, inputFolder);

                Storage.WriteAllText(filename, content);

                undo.AppendLine($"DeleteFile(filename=\"{filename}\")");
            }

            return undo.ToString();
        }

        private string GetRenderedContent(string templateContent, string templateFolder)
        {
            EnsureThat.EnsureArg.IsNotNull(Storage);

            var template = Handlebars.Compile(templateContent);

            var data = new Dictionary<string, string>();

            // Populate each file content.
            foreach (var filename in Storage.EnumerateFiles(templateFolder))
            {
                string content = Storage.ReadAllText(filename);

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
