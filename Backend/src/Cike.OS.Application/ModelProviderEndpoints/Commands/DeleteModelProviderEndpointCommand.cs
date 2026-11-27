using Cike.Cqrs.Commands;

namespace Cike.OS.Application.ModelProviderEndpoints.Commands;

public record DeleteModelProviderEndpointCommand(long Id) : Command;
