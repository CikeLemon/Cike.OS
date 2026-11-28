using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Application.Contracts.Agents.Dtos;

/// <summary>Agent 表单第二步的选择列表的一层：一个工具包及其下的工具（按「工具包 → 工具」两层组装，只含有工具的包）。</summary>
public class AgentToolPackageOptionItemDto
{
    public long ToolPackageId { get; set; }

    public string Name { get; set; } = null!;

    public List<AgentToolOptionItemDto> Tools { get; set; } = [];
}
