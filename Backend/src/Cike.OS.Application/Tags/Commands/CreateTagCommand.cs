using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.Tags.Dtos;

namespace Cike.OS.Application.Tags.Commands;

public record CreateTagCommand(CreateTagDto Dto) : Command
{
    public long Id { get; set; }
}
