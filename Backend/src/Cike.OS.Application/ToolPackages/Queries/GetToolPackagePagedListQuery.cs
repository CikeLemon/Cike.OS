using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.ToolPackages.Dtos;
using Cike.Contracts.EntityDtos;

namespace Cike.OS.Application.ToolPackages.Queries;

public record GetToolPackagePagedListQuery(GetToolPackagePagedListDto Dto) : Query<PagedResultDto<ToolPackageDto>>;
