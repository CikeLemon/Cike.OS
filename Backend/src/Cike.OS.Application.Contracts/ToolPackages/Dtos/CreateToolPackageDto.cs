using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Application.Contracts.ToolPackages.Dtos;

public class CreateToolPackageDto
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string Readme { get; set; } = string.Empty;

    public long ToolPackageCategoryId { get; set; }

    public ToolPackageStatus Status { get; set; } = ToolPackageStatus.Draft;
}
