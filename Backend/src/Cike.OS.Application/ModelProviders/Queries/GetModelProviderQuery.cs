using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;

namespace Cike.OS.Application.ModelProviders.Queries;

public record GetModelProviderQuery(long Id) : Query<ModelProviderDto?>;
