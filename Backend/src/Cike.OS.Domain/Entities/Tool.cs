using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Domain.Entities;

/// <summary>工具（聚合根）。创建走 <see cref="Create"/>，字段只能通过 <see cref="Update"/> 变更。</summary>
public class Tool : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = string.Empty;

    /// <summary>工具形态：创建后不可变。</summary>
    public ToolType Type { get; private set; }

    public long ToolPackageId { get; private set; }

    public List<ToolParameterDefinition> Parameters { get; private set; } = [];

    public ToolSetting Setting { get; private set; } = null!;

    /// <summary>是否技能形态工具。</summary>
    public bool IsSkill => Type == ToolType.Skill;

    /// <summary>
    /// 技能工具的技能包存放目录（相对存储根）；非技能工具为 null。
    /// 形态创建后不可变，所以技能工具的 Setting 恒为 <see cref="SkillToolSetting"/>。
    /// </summary>
    public string? SkillPackageDirectory => IsSkill ? ((SkillToolSetting)Setting).Directory : null;

    private Tool()
    {
    }

    private Tool(string code, string name)
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
    }

    public static Tool Create(
        string code,
        string name,
        string description,
        ToolType type,
        long toolPackageId,
        IEnumerable<ToolParameterDefinition> parameters,
        ToolSetting setting)
    {
        var tool = new Tool(code, name);
        tool.Apply(description, type, toolPackageId, parameters, setting);
        return tool;
    }

    /// <summary>
    /// 更新可变字段。形态创建后不可变：<paramref name="setting"/> 必须与现有 <see cref="Type"/> 匹配。
    /// </summary>
    public void Update(
        string code,
        string name,
        string description,
        long toolPackageId,
        IEnumerable<ToolParameterDefinition> parameters,
        ToolSetting setting)
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        Apply(description, Type, toolPackageId, parameters, setting, isUpdate: true);
    }

    /// <summary>
    /// 只断言聚合不变量（任何入口构造都必须成立）；格式 / 范围 / 枚举合法性属于输入校验，由 FluentValidation 负责。
    /// </summary>
    private void Apply(
        string description,
        ToolType type,
        long toolPackageId,
        IEnumerable<ToolParameterDefinition> parameters,
        ToolSetting setting,
        bool isUpdate = false)
    {
        AssertSettingMatchesType(type, setting, isUpdate);

        var normalizedParameters = parameters?.ToList() ?? [];
        AssertParameterCodes(normalizedParameters);

        Description = description ?? string.Empty;
        Type = type;
        ToolPackageId = toolPackageId;
        Parameters = normalizedParameters;
        Setting = setting;
    }

    /// <summary>本地不变量：设置的多态子类型必须与工具形态一一对应；形态创建后不可变。</summary>
    private static void AssertSettingMatchesType(ToolType type, ToolSetting? setting, bool isUpdate)
    {
        var matches = (type, setting) switch
        {
            (ToolType.Skill, SkillToolSetting) => true,
            (ToolType.HttpMcp, HttpMcpToolSetting) => true,
            (ToolType.StdioMcp, StdioMcpToolSetting) => true,
            _ => false,
        };

        if (!matches)
        {
            throw new UserFriendlyException(isUpdate
                ? "工具形态创建后不可修改，设置类型必须与现有形态一致。"
                : "工具形态与设置类型不匹配。");
        }
    }

    /// <summary>本地不变量：参数编码非空且不重复。</summary>
    private static void AssertParameterCodes(List<ToolParameterDefinition> parameters)
    {
        if (parameters.Any(x => string.IsNullOrWhiteSpace(x.Code)))
        {
            throw new UserFriendlyException("参数定义中的编码不能为空白。");
        }

        if (parameters.Select(x => x.Code).Distinct(StringComparer.Ordinal).Count() != parameters.Count)
        {
            throw new UserFriendlyException("参数定义中的编码不能重复。");
        }
    }

    private static string NormalizeCode(string code)
    {
        var normalized = code?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new UserFriendlyException("工具编码不能为空白。");
        }

        return normalized;
    }

    private static string NormalizeName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new UserFriendlyException("工具名称不能为空白。");
        }

        return normalized;
    }
}
