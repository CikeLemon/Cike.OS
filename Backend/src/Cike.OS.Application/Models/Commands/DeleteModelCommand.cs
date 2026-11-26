using Cike.Cqrs.Commands;

namespace Cike.OS.Application.Models.Commands;

public record DeleteModelCommand(long Id) : Command;
