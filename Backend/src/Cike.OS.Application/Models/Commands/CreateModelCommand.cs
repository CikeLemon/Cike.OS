using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.Models.Dtos;

namespace Cike.OS.Application.Models.Commands;

public record CreateModelCommand(CreateModelDto Dto) : Command
{
    public long Id { get; set; }
}
