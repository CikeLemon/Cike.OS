namespace Cike.OS.Domain.Shared.Enums;

/// <summary>
/// Agent 工具条目的类型，决定 <c>TargetId</c> 指向谁：
/// <see cref="ToolPackage"/> 表示整包绑定（包内全部工具含之后新增的都生效，不记录具体工具）；
/// <see cref="Tool"/> 表示只绑定单个工具。
/// </summary>
public enum AgentToolType
{
    ToolPackage = 1,
    Tool = 2,
}
