namespace Cike.OS.Domain.Entities;

/// <summary>
/// 模型（模型提供方聚合内的子实体）：模型提供方提供的一个模型——名称（调用时原样发给提供方）、可选描述与标签。
/// 走 <see cref="Create"/> 构造，加进哪个提供方、加不加由 <see cref="ModelProvider"/> 决定；没有修改名称的方法，名称创建后不可变。
/// 非空 / 长度等单字段规则一律在 FluentValidation，这里只做标签引用去重。
/// </summary>
public class ModelEntity : FullAuditedEntity<long>, IMultiTenant
{
    public long TenantId { get; set; }

    /// <summary>所属的模型提供方。</summary>
    public long ModelProviderId { get; private set; }

    /// <summary>模型名称（如 glm-5.3）：调用时原样发给提供方，创建后不可变；同一提供方下不重复。</summary>
    public string Name { get; private set; } = null!;

    /// <summary>描述：可选，未填写为空字符串。</summary>
    public string Description { get; private set; } = string.Empty;

    public List<long> TagIds { get; private set; } = [];

    private ModelEntity()
    {
    }

    public static ModelEntity Create(string name, string? description, IEnumerable<long>? tagIds)
    {
        return new ModelEntity
        {
            Name = name,
            Description = description ?? string.Empty,
            TagIds = tagIds?.Distinct().ToList() ?? [],
        };
    }

    /// <summary>更新可变字段（描述 / 标签）；名称创建后不可变，不在修改之列。</summary>
    internal void Update(string? description, IEnumerable<long>? tagIds)
    {
        Description = description ?? string.Empty;
        TagIds = tagIds?.Distinct().ToList() ?? [];
    }
}
