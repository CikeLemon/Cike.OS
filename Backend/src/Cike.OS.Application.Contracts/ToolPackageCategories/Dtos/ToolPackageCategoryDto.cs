using Cike.Contracts.EntityDtos;

namespace Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;

public class ToolPackageCategoryDto : AuditedEntityDto<long>
{
    public string Name { get; set; } = null!;
}
