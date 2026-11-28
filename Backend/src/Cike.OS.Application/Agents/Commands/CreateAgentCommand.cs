using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.Agents.Dtos;

namespace Cike.OS.Application.Agents.Commands;

public record CreateAgentCommand(CreateAgentDto Dto) : Command
{
    public long Id { get; set; }
}
