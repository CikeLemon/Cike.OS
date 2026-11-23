namespace Cike.OS.Domain.Entities;

public class AgentTool : FullAuditedEntity<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public long AgentToolPackageId { get; set; }

    public long ToolId { get; set; }
}
