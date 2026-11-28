using Cike.Contracts.EntityDtos;

namespace Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;

/// <summary>工具包分类详情：编辑表单回填用。</summary>
public class ToolPackageCategoryDetailDto : AuditedEntityDto<long>
{
    public string Name { get; set; } = null!;
}
