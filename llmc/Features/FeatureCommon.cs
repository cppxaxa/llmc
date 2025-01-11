using llmc.Connector;
using llmc.Project;
using llmc.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Features;

internal abstract class FeatureCommon : IFeature
{
    public bool VerboseLogging { get; set; } = false;
    public bool NoUndo { get; set; } = false;
    public LlmConnector? Connector { get; set; }
    public Prompt? Prompt { get; set; }
    public ExecutorFinder? ExecutorFinder { get; set; }
    public ExecutorInvoker? ExecutorInvoker { get; set; }
    public FileRedactor? FileRedactor { get; set; }
    public IStorage? Storage { get; set; }

    public virtual bool AsPrebuild { get; set; } = false;

    abstract public FeatureResult Execute(
        string parentDirectory, string param);
}
