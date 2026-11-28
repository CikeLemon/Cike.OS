using Cike.Contracts.EntityDtos;

namespace Cike.OS.Application.Contracts.Agents.Dtos;

/// <summary>Agent 列表的一行：界面要什么给什么，不是详情 DTO 的复用。</summary>
public class AgentItemDto : AuditedEntityDto<long>
{
    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
}
