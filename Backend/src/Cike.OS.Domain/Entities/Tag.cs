namespace Cike.OS.Domain.Entities;

/// <summary>标签（平台级目录实体，无租户）：以键为标识的标注词条，用于模型等平台目录资源的标记。</summary>
public class Tag : FullAuditedAggregateRoot<long>
{
    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;
}
