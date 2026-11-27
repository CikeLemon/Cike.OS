using Cike.Cqrs.Commands;

namespace Cike.OS.Application.ModelProviders.Commands;

public record DeleteModelProviderEndpointCommand(long ProviderId, long EndpointId) : Command;
