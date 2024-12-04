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

    // Returns command to undo.
    List<List<ExecutorFinderResult>> Execute(
        string parentDirectory, string param);
}
