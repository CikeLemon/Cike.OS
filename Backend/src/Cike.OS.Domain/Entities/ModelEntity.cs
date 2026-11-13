namespace Cike.OS.Domain.Entities;

public class ModelEntity : AuditedEntity<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public long ModelProviderId { get; set; }

    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public List<long> TagIds { get; set; } = [];
}
