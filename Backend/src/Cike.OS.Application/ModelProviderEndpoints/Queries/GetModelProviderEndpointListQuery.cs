using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;

namespace Cike.OS.Application.ModelProviderEndpoints.Queries;

/// <summary>某个提供方名下的全部接入点；按提供方 Id 过滤，提供方不存在时就是空列表。</summary>
public record GetModelProviderEndpointListQuery(long ModelProviderId) : Query<List<ModelProviderEndpointDto>>;
