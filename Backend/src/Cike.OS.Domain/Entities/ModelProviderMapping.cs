namespace Cike.OS.Domain.Entities;

/// <summary>
/// 提供商映射（模型提供方聚合内的子实体）：这个提供方接入了哪个模型、以多少分数参与路由、在提供商侧叫什么名字。
/// 没有独立用例与对外接口，随 <see cref="ModelProvider.SyncMappings"/> 整体维护。
/// 非空 / 范围等单字段规则一律在 FluentValidation，这里不做校验。
/// </summary>
public class ModelProviderMapping : FullAuditedEntity<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public long ModelProviderId { get; private set; }

    public long ModelId { get; private set; }

    /// <summary>路由分数：只是排序依据，不要求唯一；同一个模型被多个提供方接入时，取分数最高的映射。</summary>
    public int Priority { get; private set; }

    /// <summary>提供商侧模型名（聚合平台可能改名）；空字符串表示回落用 <see cref="ModelEntity.Name"/>。</summary>
    public string ProviderModelName { get; private set; } = string.Empty;

    private ModelProviderMapping()
    {
    }

    // 只允许聚合根创建与修改子实体，Handler 不直接碰
    internal static ModelProviderMapping Create(long modelId, int priority, string? providerModelName)
    {
        return new ModelProviderMapping
        {
            ModelId = modelId,
            Priority = priority,
            ProviderModelName = providerModelName ?? string.Empty,
        };
    }

    internal void Update(int priority, string? providerModelName)
    {
        Priority = priority;
        ProviderModelName = providerModelName ?? string.Empty;
    }
}
