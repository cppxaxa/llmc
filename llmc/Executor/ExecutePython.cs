using llmc.Project;
using Python.Included;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Executor
{
    internal class ExecutePython : IExecutor
    {
        public string Execute(string parentDirectory, string param)
        {
            Dictionary<string, string> p = Common.ParseParam(param);

            string? filename = p.ContainsKey("filename") ? p["filename"] : null;
            string? pythonCode = p.ContainsKey("pythonCode") ? p["pythonCode"] : null;

            string originalWorkingDirectory = Environment.CurrentDirectory;
            string code = string.Empty;

            try
            {
                Environment.CurrentDirectory = parentDirectory;

                if (!string.IsNullOrEmpty(filename))
                {
                    code = File.ReadAllText(Path.Join(parentDirectory, filename), Encoding.UTF8);
                }
                else if (!string.IsNullOrEmpty(pythonCode))
                {
                    code = pythonCode;
                }
                else
                {
                    return string.Empty;
                }

                Installer.SetupPython().GetAwaiter().GetResult();
                Console.WriteLine($"Is Python installed: {Installer.IsPythonInstalled()}");
                PythonEngine.Initialize();
                int result = PythonEngine.RunSimpleString(code);
                Console.WriteLine($"Python returned: {result}");
                PythonEngine.Shutdown();
            }
            finally
            {
                Environment.CurrentDirectory = originalWorkingDirectory;
            }

            return string.Empty;
        }
    }
}
