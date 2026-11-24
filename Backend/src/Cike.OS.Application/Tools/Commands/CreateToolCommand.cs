using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.Tools.Dtos;

namespace Cike.OS.Application.Tools.Commands;

public record CreateToolCommand(CreateToolDto Dto) : Command
{
    public long Id { get; set; }
}
