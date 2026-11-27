namespace Cike.OS.Caching.Dtos;

public class ModelCacheDto : AuditedEntityDto<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public long ModelProviderId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public List<long> TagIds { get; set; } = [];
}
