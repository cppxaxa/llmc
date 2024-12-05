using llmc.Connector;
using llmc.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Features;

internal abstract class FeatureCommon : IFeature
{
    public LlmConnector? Connector { get; set; }
    public Prompt? Prompt { get; set; }
    public ExecutorFinder? ExecutorFinder { get; set; }
    public ExecutorInvoker? ExecutorInvoker { get; set; }

    abstract public void Execute(
        string parentDirectory, string param);
}
