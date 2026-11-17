using Cike.Cqrs.Commands;

namespace Cike.OS.Application.UserToolConfigurations.Commands;

public record DeleteUserToolConfigurationCommand(long ToolId) : Command;
