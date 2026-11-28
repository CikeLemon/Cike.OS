using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Domain.Entities;

/// <summary>
/// Agent 工具（Agent 聚合内的子实体）：Agent 绑定的一个工具条目。
/// <see cref="AgentToolType.ToolPackage"/> 表示整包绑定，<see cref="TargetId"/> 指向工具包；
/// <see cref="AgentToolType.Tool"/> 表示单个工具，<see cref="TargetId"/> 指向工具。
/// 走 <see cref="Create"/> 构造，加进哪个 Agent 由 <see cref="AgentEntity"/> 决定；同步即替换，没有修改方法。
/// </summary>
public class AgentTool : FullAuditedEntity<long>, IMultiTenant
{
    public long TenantId { get; set; }

    /// <summary>所属的 Agent。</summary>
    public long AgentId { get; private set; }

    public AgentToolType Type { get; private set; }

    public long TargetId { get; private set; }

    private AgentTool()
    {
    }

    public static AgentTool Create(AgentToolType type, long targetId)
    {
        return new AgentTool
        {
            Type = type,
            TargetId = targetId,
        };
    }
}
