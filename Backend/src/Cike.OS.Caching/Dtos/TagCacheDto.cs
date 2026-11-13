namespace Cike.OS.Caching.Dtos;

public class TagCacheDto : FullAuditedEntityDto<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;
}
