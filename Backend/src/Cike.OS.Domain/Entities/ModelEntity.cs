namespace Cike.OS.Domain.Entities;

/// <summary>
/// 模型（平台级目录聚合根，无租户）：定义市面上的模型本身（如 glm-5.3、qwen3.8）。
/// 创建走 <see cref="Create"/>；模型名称创建后不可变，其余字段只能通过 <see cref="Update"/> 变更；提供商接入见 <see cref="ModelProviderMapping"/>。
/// 非空 / 长度等单字段规则一律在 FluentValidation，这里不做校验。
/// </summary>
public class ModelEntity : FullAuditedAggregateRoot<long>
{
    /// <summary>模型名称（如 glm-5.3）：创建后不可变，全局唯一。</summary>
    public string Name { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    /// <summary>描述：可选，未填写为空字符串。</summary>
    public string Description { get; private set; } = string.Empty;

    public List<long> TagIds { get; private set; } = [];

    private ModelEntity()
    {
    }

    public static ModelEntity Create(string name, string displayName, string? description, IEnumerable<long> tagIds)
    {
        return new ModelEntity
        {
            Name = name,
            DisplayName = displayName,
            Description = description ?? string.Empty,
            TagIds = tagIds?.Distinct().ToList() ?? [],
        };
    }

    /// <summary>更新可变字段（显示名称 / 描述 / 标签）；模型名称创建后不可变。</summary>
    public void Update(string displayName, string? description, IEnumerable<long> tagIds)
    {
        DisplayName = displayName;
        Description = description ?? string.Empty;
        TagIds = tagIds?.Distinct().ToList() ?? [];
    }
}
