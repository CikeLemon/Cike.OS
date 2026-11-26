namespace Cike.OS.Domain.Entities;

/// <summary>
/// 模型提供方（租户级聚合根）：持有连接设置，以及它接入的模型（<see cref="Mappings"/>，聚合内子实体）。
/// 映射随提供方整体维护，没有独立的用例与对外接口。
/// </summary>
public class ModelProvider : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Name { get; set; } = null!;

    public ModelProviderSetting Setting { get; set; } = null!;

    public List<ModelProviderMapping> Mappings { get; private set; } = [];

    /// <summary>
    /// 按提交的映射整体同步（以模型为键）：已映射的模型更新分数与提供商侧模型名，新出现的模型新增，未提交的移除。
    /// 一个提供方对一个模型只保留一条映射是聚合内部不变量，输入里重复的模型在这里被拒。
    /// </summary>
    public void SyncMappings(IEnumerable<(long ModelId, int Priority, string? ProviderModelName)> mappings)
    {
        var submitted = new List<(long ModelId, int Priority, string? ProviderModelName)>();
        foreach (var mapping in mappings)
        {
            if (submitted.Any(x => x.ModelId == mapping.ModelId))
            {
                throw new UserFriendlyException("同一个模型在提供方下只能映射一次。");
            }

            submitted.Add(mapping);
        }

        var modelIds = submitted.Select(x => x.ModelId).ToHashSet();

        foreach (var mapping in Mappings.Where(x => !modelIds.Contains(x.ModelId)).ToList())
        {
            Mappings.Remove(mapping);
        }

        foreach (var mapping in submitted)
        {
            var existing = Mappings.FirstOrDefault(x => x.ModelId == mapping.ModelId);
            if (existing == null)
            {
                Mappings.Add(ModelProviderMapping.Create(mapping.ModelId, mapping.Priority, mapping.ProviderModelName));
            }
            else
            {
                existing.Update(mapping.Priority, mapping.ProviderModelName);
            }
        }
    }
}
