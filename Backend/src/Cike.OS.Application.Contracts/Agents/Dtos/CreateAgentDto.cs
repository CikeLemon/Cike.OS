using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.Agents.Dtos;

/// <summary>Agent 的新建输入：全部字段与期望的完整工具条目列表。</summary>
public class CreateAgentDto
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Instructions { get; set; } = null!;

    public AgentOptions Options { get; set; } = new();

    /// <summary>提交的是期望的完整工具条目列表，没提交的条目会被移除。</summary>
    public List<SaveAgentToolDto> Tools { get; set; } = [];
}
