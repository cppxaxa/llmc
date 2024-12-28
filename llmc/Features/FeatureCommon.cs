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
    public bool NoUndo { get; set; } = false;
    public LlmConnector? Connector { get; set; }
    public Prompt? Prompt { get; set; }
    public ExecutorFinder? ExecutorFinder { get; set; }
    public ExecutorInvoker? ExecutorInvoker { get; set; }
    public FileRedactor? FileRedactor { get; set; }

    public virtual bool AsPrebuild { get; set; } = false;

    abstract public void Execute(
        string parentDirectory, string param);
}
