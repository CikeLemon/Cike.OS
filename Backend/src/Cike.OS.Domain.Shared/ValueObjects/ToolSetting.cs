using ModelContextProtocol.Client;

namespace Cike.OS.Domain.Shared.ValueObjects;

/// <summary>
/// 工具设置（多态值对象）。位于 Domain.Shared，直接作为 API 契约与持久化形状：
/// 判别键为 <c>type</c>，取值与 <see cref="ToolType"/> 一致。
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SkillToolSetting), (int)ToolType.Skill)]
[JsonDerivedType(typeof(HttpMcpToolSetting), (int)ToolType.HttpMcp)]
[JsonDerivedType(typeof(StdioMcpToolSetting), (int)ToolType.StdioMcp)]
public abstract class ToolSetting
{
}

public class SkillToolSetting : ToolSetting
{
    /// <summary>技能包存放位置：相对存储根的路径，由技能包上传接口返回。</summary>
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

    public IDictionary<string, ToolValue>? AdditionalHeaders { get; set; }
}

public class StdioMcpToolSetting : ToolSetting
{
    public string Name { get; set; } = string.Empty;

    public string Command { get; set; } = null!;

    public IList<string>? Arguments { get; set; }

    public string? WorkingDirectory { get; set; }

    public IDictionary<string, ToolValue>? EnvironmentVariables { get; set; }
}
