namespace Cike.OS.Domain.Entities;

/// <summary>
/// 标签（平台级目录实体，无租户）：以键为标识的标注词条，用于模型等平台目录资源的标记。
/// 创建走 <see cref="Create"/>；键创建后不可变，名称通过 <see cref="Rename"/> 变更。
/// 非空 / 格式 / 长度等单字段规则一律在 FluentValidation，这里不做校验。
/// </summary>
public class Tag : FullAuditedAggregateRoot<long>
{
    /// <summary>机器标识（如 vision）：全局唯一，创建后不可变。</summary>
    public string Key { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    private Tag()
    {
    }

    public static Tag Create(string key, string name)
    {
        return new Tag
        {
            Key = key,
            Name = name,
        };
    }

    /// <summary>改名；键不可变，没有对应的修改方法。</summary>
    public void Rename(string name)
    {
        Name = name;
    }
}
