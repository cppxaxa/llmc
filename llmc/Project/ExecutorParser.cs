using System.Text.RegularExpressions;

namespace llmc.Project
{
    internal class ExecutorParser
    {
        public static (string className, string param) Parse(string line)
        {
            string className = line.Split('(')[0];
            string param = string.Empty;
            string pattern = @"\((.*)\)";

            Match match = Regex.Match(line, pattern);

            if (match.Success)
            {
                param = match.Groups[1].Value;
            }

            return (className, param);
        }
    }
}