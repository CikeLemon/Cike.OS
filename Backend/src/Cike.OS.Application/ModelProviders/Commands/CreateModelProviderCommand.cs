using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;

namespace Cike.OS.Application.ModelProviders.Commands;

public record CreateModelProviderCommand(CreateModelProviderDto Dto) : Command
{
    public long Id { get; set; }
}
