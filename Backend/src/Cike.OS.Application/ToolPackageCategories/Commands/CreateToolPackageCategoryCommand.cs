using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;

namespace Cike.OS.Application.ToolPackageCategories.Commands;

public record CreateToolPackageCategoryCommand(CreateToolPackageCategoryDto Dto) : Command
{
    public long Id { get; set; }
}
