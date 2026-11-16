using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;

namespace Cike.OS.Domain.Shared.ValueObjects;

public abstract class ToolSetting
{
}

public class SkillToolSetting : ToolSetting
{
    public string Directory { get; set; } = string.Empty;
}

public class HttpMcpToolSetting : ToolSetting
{
    public string Name { get; set; } = string.Empty;

    public string Endpoint { get; set; } = null!;

    public HttpTransportMode TransportMode { get; set; } = HttpTransportMode.StreamableHttp;

    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public int MaxReconnectionAttempts { get; set; } = 5;

    public TimeSpan DefaultReconnectionInterval { get; set; } = TimeSpan.FromSeconds(1);

    public string? CredentialKey { get; set; }

    public IDictionary<string, string>? AdditionalHeaders { get; set; }
}

public class StdioMcpToolSetting : ToolSetting
{
    public string Name { get; set; } = string.Empty;

    public string Command { get; set; } = null!;

    public IList<string>? Arguments { get; set; }

    public string? WorkingDirectory { get; set; }

    public IDictionary<string, string?>? EnvironmentVariables { get; set; }
}
