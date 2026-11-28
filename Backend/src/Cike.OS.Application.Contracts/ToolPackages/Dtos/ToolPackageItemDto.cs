using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Application.Contracts.ToolPackages.Dtos;

/// <summary>工具包分页列表的一行：列表页显示什么给什么。</summary>
public class ToolPackageItemDto : AuditedEntityDto<long>
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    /// <summary>所属分类名称，由查询侧回填。</summary>
    public string ToolPackageCategoryName { get; set; } = string.Empty;

    public ToolPackageStatus Status { get; set; }
}
