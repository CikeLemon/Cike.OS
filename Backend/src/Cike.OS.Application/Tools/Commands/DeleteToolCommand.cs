using Cike.Cqrs.Commands;

namespace Cike.OS.Application.Tools.Commands;

public record DeleteToolCommand(long Id) : Command;
