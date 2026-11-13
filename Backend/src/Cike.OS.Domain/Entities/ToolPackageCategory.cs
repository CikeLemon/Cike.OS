namespace Cike.OS.Domain.Entities;

public class ToolPackageCategory : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Name { get; set; } = null!;
}
