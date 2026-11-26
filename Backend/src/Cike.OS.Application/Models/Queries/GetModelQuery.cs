using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.Models.Dtos;

namespace Cike.OS.Application.Models.Queries;

public record GetModelQuery(long Id) : Query<ModelDto?>;
