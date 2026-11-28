using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Application.Contracts.Agents.Dtos;

/// <summary>Agent 表单第二步里可选的一个工具。</summary>
public class AgentToolOptionItemDto
{
    public long ToolId { get; set; }

    public string Name { get; set; } = null!;

    public ToolType Type { get; set; }

    /// <summary>声明的参数个数（供前端判定「新建后要不要跳去填参数」）。</summary>
    public int ParameterCount { get; set; }
}
