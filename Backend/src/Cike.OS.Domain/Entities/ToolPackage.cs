using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Domain.Entities;

/// <summary>工具包（聚合根）。创建走 <see cref="Create"/>，字段只能通过 <see cref="Update"/> 变更。</summary>
public class ToolPackage : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = string.Empty;

    public string Icon { get; private set; } = string.Empty;

    public string Readme { get; private set; } = string.Empty;

    public long ToolPackageCategoryId { get; private set; }

    public ToolPackageStatus Status { get; private set; }

    private ToolPackage()
    {
    }

    private ToolPackage(string code, string name)
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
    }

    public static ToolPackage Create(
        string code,
        string name,
        string description,
        string icon,
        string readme,
        long toolPackageCategoryId,
        ToolPackageStatus status)
    {
        var package = new ToolPackage(code, name);
        package.Apply(description, icon, readme, toolPackageCategoryId, status);
        return package;
    }

    public void Update(
        string code,
        string name,
        string description,
        string icon,
        string readme,
        long toolPackageCategoryId,
        ToolPackageStatus status)
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        Apply(description, icon, readme, toolPackageCategoryId, status);
    }

    /// <summary>
    /// 只断言聚合不变量；分类引用是否有效、状态是否在枚举范围内属于输入 / 跨聚合校验，
    /// 分别由 FluentValidation 与 Handler 负责。
    /// </summary>
    private void Apply(string description, string icon, string readme, long toolPackageCategoryId, ToolPackageStatus status)
    {
        Description = description ?? string.Empty;
        Icon = icon?.Trim() ?? string.Empty;
        Readme = readme ?? string.Empty;
        ToolPackageCategoryId = toolPackageCategoryId;
        Status = status;
    }

    private static string NormalizeCode(string code)
    {
        var normalized = code?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new UserFriendlyException("工具包编码不能为空白。");
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new UserFriendlyException("工具包名称不能为空白。");
        }

        return normalized;
    }
}
