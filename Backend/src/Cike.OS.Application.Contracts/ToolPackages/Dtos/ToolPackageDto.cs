using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Application.Contracts.ToolPackages.Dtos;

public class ToolPackageDto : AuditedEntityDto<long>
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string Readme { get; set; } = string.Empty;

    public long ToolPackageCategoryId { get; set; }

    /// <summary>所属分类名称，由查询侧回填，便于列表与详情直接展示。</summary>
    public string ToolPackageCategoryName { get; set; } = string.Empty;

    public ToolPackageStatus Status { get; set; }
}
