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
    internal class TaskDelay : ExecutorCommon
    {
        public override string Execute(string parentDirectory, string param)
        {
            StringBuilder undo = new();

            Dictionary<string, string> p = Common.ParseParam(param);
            string millis = p["millis"];

            Console.WriteLine($"TaskDelay:Sleeping for {millis} milliseconds.");

            // Sleep the thread.
            Thread.Sleep(int.Parse(millis));

            Console.WriteLine($"TaskDelay:Woke up after {millis} milliseconds.");

            return undo.ToString();
        }
    }
}
