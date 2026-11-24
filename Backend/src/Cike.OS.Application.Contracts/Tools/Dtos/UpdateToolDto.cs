using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.Tools.Dtos;

/// <summary>
/// 更新工具的可变字段（Id 在路由）。形态（Type）创建后不可变，不在更新契约里；
/// 设置的多态子类型必须与工具现有形态一致，由聚合根断言。
/// </summary>
public class UpdateToolDto
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public long ToolPackageId { get; set; }

    public List<ToolParameterDefinition> Parameters { get; set; } = [];

    public ToolSetting Setting { get; set; } = null!;
}
