using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.Agents.Dtos;

/// <summary>Agent 详情：基本信息、模型选项（值对象直接作契约）与它绑定的工具条目。</summary>
public class AgentDetailDto : AuditedEntityDto<long>
{
    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    /// <summary>指令：Agent 的自我介绍与提示词。</summary>
    public string Instructions { get; set; } = null!;

    public AgentOptions Options { get; set; } = new();

    public List<AgentToolItemDto> Tools { get; set; } = [];
}
