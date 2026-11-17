using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;

namespace Cike.OS.Application.UserToolConfigurations.Commands;

public record SaveUserToolConfigurationCommand(SaveUserToolConfigurationDto Dto) : Command
{
    public long ToolId { get; set; }
}
