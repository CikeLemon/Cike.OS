using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;

namespace Cike.OS.Application.ModelProviders.Queries;

/// <summary>某个提供方名下的全部接入点；提供方不存在时结果为 null。</summary>
public record GetModelProviderEndpointListQuery(long ProviderId) : Query<List<ModelProviderEndpointDto>?>;
