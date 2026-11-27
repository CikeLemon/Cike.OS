using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;

namespace Cike.OS.Application.ModelProviderEndpoints.Queries;

/// <summary>一个接入点的详情；不存在（含不属于本租户）时结果为 null。</summary>
public record GetModelProviderEndpointQuery(long Id) : Query<ModelProviderEndpointDto?>;
