using Cike.Cqrs.Commands;

namespace Cike.OS.Application.ToolPackages.Commands;

public record DeleteToolPackageCommand(long Id) : Command;
