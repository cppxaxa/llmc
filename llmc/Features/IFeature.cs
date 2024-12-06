using llmc.Connector;
using llmc.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Features;

internal interface IFeature
{
    LlmConnector? Connector { get; set; }

    Prompt? Prompt { get; set; }

    ExecutorFinder? ExecutorFinder { get; set; }

    ExecutorInvoker? ExecutorInvoker { get; set; }
    
    FileRedactor? FileRedactor { get; set; }

    // Returns command to undo.
    void Execute(string parentDirectory, string param);
}
