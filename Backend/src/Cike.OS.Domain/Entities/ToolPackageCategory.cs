namespace Cike.OS.Domain.Entities;

/// <summary>工具包分类（聚合根）。创建走 <see cref="Create"/>，名称只能通过 <see cref="Rename"/> 变更。</summary>
public class ToolPackageCategory : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Name { get; private set; } = null!;

    private ToolPackageCategory()
    {
    }

    private ToolPackageCategory(string name)
    {
        Name = name;
    }

    public static ToolPackageCategory Create(string name)
    {
        return new ToolPackageCategory(NormalizeName(name));
    }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
    }

    private static string NormalizeName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new UserFriendlyException("工具包分类名称不能为空白。");
        }

        return normalized;
    }
}
