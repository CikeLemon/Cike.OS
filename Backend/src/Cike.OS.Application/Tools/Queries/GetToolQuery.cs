using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.Tools.Dtos;

namespace Cike.OS.Application.Tools.Queries;

public record GetToolQuery(long Id) : Query<ToolDto?>;
