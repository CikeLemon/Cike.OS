using Cike.Cqrs.Commands;

namespace Cike.OS.Application.ModelProviders.Commands;

public record DeleteModelProviderCommand(long Id) : Command;
