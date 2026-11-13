namespace Cike.OS.Caching.Dtos;

public class ModelProviderCacheDto : FullAuditedEntityDto<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Name { get; set; } = null!;

    public ModelProviderType Type { get; set; }

    public ModelProviderSetting Setting { get; set; } = null!;
}
