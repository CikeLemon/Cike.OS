using Cike.Contracts.EntityDtos;

namespace Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;

/// <summary>工具包分类列表的一行（分类管理页；工具包列表的筛选与表单的分类下拉也用它）。</summary>
public class ToolPackageCategoryItemDto : AuditedEntityDto<long>
{
    public string Name { get; set; } = null!;
}
