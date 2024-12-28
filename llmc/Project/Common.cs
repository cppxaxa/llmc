using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Project
{
    internal static class Common
    {
        public static string FindLineSeparator(string rawPrompt)
        {
            if (rawPrompt.Contains("\r\n"))
            {
                return "\r\n";
            }
            else if (rawPrompt.Contains("\n"))
            {
                return "\n";
            }
            else if (rawPrompt.Contains("\r"))
            {
                return "\r";
            }
            else
            {
                return Environment.NewLine;
            }
        }

        internal static string ApplyProjectMacros(ProjectModel project, string fileContent)
        {
            foreach (var promptMacro in project.Macros)
            {
                fileContent = fileContent.Replace(promptMacro.Key, promptMacro.Value);
            }

            return fileContent;
        }

        internal static Dictionary<string, string> ParseParam(string param)
        {
            string somewhatJson = param.Replace("=\"", ":\"");
            string json = $"{{{somewhatJson}}}";
            Dictionary<string, string> p = JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                ?? throw new ArgumentException("Malformed parameter format.");
            return p ?? new Dictionary<string, string>();
        }
    }
}
