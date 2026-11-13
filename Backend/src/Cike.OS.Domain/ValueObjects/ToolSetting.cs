using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cike.OS.Domain.Shared.ValueObjects;

public abstract class ToolSetting
{
}

public class SkillToolSetting : ToolSetting
{
    public string Directory { get; set; } = null!;
}

public class HttpMcpToolSetting : ToolSetting
{
    public string Name { get; set; } = string.Empty;

    public string Endpoint { get; set; } = null!;

    public HttpTransportMode TransportMode { get; set; }

    public int Timeout { get; set; } = 30;

    public IDictionary<string, string>? AdditionalHeaders { get; set; }
}

public class StdioMcpToolSetting : ToolSetting
{
}
