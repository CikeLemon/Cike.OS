using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Application.Contracts.Agents.Dtos;

/// <summary>Agent 详情里绑定的一个工具条目（聚合内子实体）：整包（指向工具包）或单个工具。</summary>
public class AgentToolItemDto
{
    public AgentToolType Type { get; set; }

    /// <summary>指向谁由 <see cref="Type"/> 决定：工具包 Id 或工具 Id。</summary>
    public long TargetId { get; set; }
}
