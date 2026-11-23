namespace Cike.OS.Domain.Entities;

public class AgentToolPackage : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public long AgentId { get; set; }

    public long ToolPackageId { get; set; }

    public bool IsFull { get; set; }

    public List<AgentTool> AgentTools { get; set; } = [];
}
