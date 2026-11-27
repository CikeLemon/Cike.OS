namespace Cike.OS.Domain.Entities;

/// <summary>
/// 模型提供方（租户级聚合根）：一个供应商在本租户下的条目，本身只有名称。
/// 它的接入点是各自独立的聚合，只按 Id 归属于这里（见 <see cref="ModelProviderEndpoint.ModelProviderId"/>），本聚合不持有接入点集合。
/// </summary>
public class ModelProvider : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Name { get; private set; } = null!;

    private ModelProvider()
    {
    }

    private ModelProvider(string name)
    {
        Name = name;
    }

    public static ModelProvider Create(string name)
    {
        return new ModelProvider(name);
    }

    public void Rename(string name)
    {
        Name = name;
    }
}
