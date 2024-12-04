using llmc.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Executor
{
    internal class ExecutePwsh : ExecutorCommon
    {
        public override string Execute(string parentDirectory, string param)
        {
            Console.WriteLine("Executing command: " + param);

            return string.Empty;
        }
    }
}
