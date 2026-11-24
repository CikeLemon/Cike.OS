using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.ToolPackages.Dtos;

namespace Cike.OS.Application.ToolPackages.Queries;

public record GetToolPackageQuery(long Id) : Query<ToolPackageDto?>;
