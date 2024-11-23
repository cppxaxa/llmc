using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llmc.Connector;

internal class LlmConnector(Configuration configuration)
{
    public string Complete(string prompt)
    {
        var client = GetClient();
        return client.Complete(prompt);
    }

    private Client GetClient()
    {
        return new Client(configuration);
    }
}
