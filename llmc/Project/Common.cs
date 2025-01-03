using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        internal static bool IsWildcardMatch(string fileName, string wildcardPattern)
        {
            // Escape special regex characters in the pattern, then replace wildcards with regex equivalents
            string regexPattern = "^" + Regex.Escape(wildcardPattern)
                .Replace(@"\*", ".*")   // Replace '*' with '.*' (match zero or more characters)
                .Replace(@"\?", ".")    // Replace '?' with '.' (match any single character)
                + "$";

            // Match the filename against the regex pattern
            return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
        }

        internal static void MonitorProcess(int pid)
        {
            while (true)
            {
                if (!IsProcessAlive(pid))
                {
                    Console.WriteLine("Monitored process is no longer alive. Exiting...");
                    Environment.Exit(0); // Terminate the entire program
                }

                Thread.Sleep(10000); // Check every 1 second
            }
        }

        internal static bool IsProcessAlive(int pid)
        {
            try
            {
                Process process = Process.GetProcessById(pid);
                return !process.HasExited; // Check explicitly if it has exited
            }
            catch (ArgumentException)
            {
                // Process does not exist
                return false;
            }
            catch (InvalidOperationException)
            {
                // Process is no longer running
                return false;
            }
        }

        internal static Dictionary<string, string> ParseParam(string param)
        {
            string somewhatJson = param.Replace("=\"", ":\"");
            string json = $"{{{somewhatJson}}}";
            Dictionary<string, string> p = JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                ?? throw new ArgumentException("Malformed parameter format.");
            return p ?? new Dictionary<string, string>();
        }

        internal static bool TryParseJson<T>(string value, out T? projectModel)
        {
            try
            {
                projectModel = JsonConvert.DeserializeObject<T>(value);
                return true;
            }
            catch
            {
                projectModel = default;
                return false;
            }
        }
    }
}
