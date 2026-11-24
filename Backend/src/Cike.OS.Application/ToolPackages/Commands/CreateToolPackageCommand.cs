using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.ToolPackages.Dtos;

namespace Cike.OS.Application.ToolPackages.Commands;

public record CreateToolPackageCommand(CreateToolPackageDto Dto) : Command
{
    public long Id { get; set; }
}
