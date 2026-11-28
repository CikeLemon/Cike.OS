using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.Tags.Dtos;

namespace Cike.OS.Application.Tags.Queries;

public record GetTagListQuery : Query<List<TagItemDto>>;
