using llmc.Connector;
using llmc.Project;
using llmc.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Features;

internal interface IFeature
{
    bool VerboseLogging { get; set; }

    bool NoUndo { get; set; }

    LlmConnector? Connector { get; set; }

    Prompt? Prompt { get; set; }

    ExecutorFinder? ExecutorFinder { get; set; }

    ExecutorInvoker? ExecutorInvoker { get; set; }
    
    FileRedactor? FileRedactor { get; set; }

    bool AsPrebuild { get; set; }

    IStorage? Storage { get; set; }

    // Returns command to undo.
    void Execute(string parentDirectory, string param);
}
