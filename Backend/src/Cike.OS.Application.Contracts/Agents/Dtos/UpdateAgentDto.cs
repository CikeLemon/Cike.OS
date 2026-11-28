using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.Agents.Dtos;

/// <summary>Agent 的更新输入：没有不可变字段，与新建同形；Id 在路由。</summary>
public class UpdateAgentDto
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Instructions { get; set; } = null!;

    public AgentOptions Options { get; set; } = new();

    public List<SaveAgentToolDto> Tools { get; set; } = [];
}
