using Cike.Cqrs.Queries;
using Cike.Contracts.EntityDtos;
using Cike.OS.Application.Contracts.Models.Dtos;

namespace Cike.OS.Application.Models.Queries;

public record GetModelPagedListQuery(GetModelPagedListDto Dto) : Query<PagedResultDto<ModelDto>>;
