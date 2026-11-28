using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;

namespace Cike.OS.Application.ModelProviders.Queries;

public record GetModelProviderDetailQuery(long Id) : Query<ModelProviderDetailDto?>;
