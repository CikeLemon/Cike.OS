namespace Cike.OS.Domain.Entities;

public class Tag : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;
}
