using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;
using Cike.OS.Application.ModelProviderEndpoints.Queries;
using Cike.OS.Domain.Entities;
using Mapster;

namespace Cike.OS.Application.ModelProviders;

/// <summary>接入点查询：列表按提供方过滤、单条详情；都是只读加载接入点连同映射，实体 → DTO 交给 Mapster，手写的只有排序与模型显示名称回填。</summary>
public class ModelProviderEndpointQueryHandler(
    IReadOnlyRepository<ModelProviderEndpoint, long> endpointRepository,
    IReadOnlyRepository<ModelEntity, long> modelRepository)
{
    [LocalEventHandler]
    public async Task GetListAsync(GetModelProviderEndpointListQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = endpointRepository.BeginAsNoTracking();
        var endpoints = await endpointRepository.ToListAsync(
            endpointRepository.WithDetails(x => x.Mappings).Where(x => x.ModelProviderId == query.ModelProviderId),
            cancellationToken);
        query.Result = await ToDtosAsync(endpoints, cancellationToken);
    }

    [LocalEventHandler]
    public async Task GetAsync(GetModelProviderEndpointQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = endpointRepository.BeginAsNoTracking();
        var endpoints = await endpointRepository.ToListAsync(
            endpointRepository.WithDetails(x => x.Mappings).Where(x => x.Id == query.Id),
            cancellationToken);
        query.Result = (await ToDtosAsync(endpoints, cancellationToken)).FirstOrDefault();
    }

    /// <summary>
    /// 接入点按名称排序（多态设置两边声明类型相同，Mapster 直接引用带出），接入点下的映射按分数降序（与路由取用顺序一致），
    /// 模型显示名称一次批量取回填，不逐行查库。
    /// </summary>
    private async Task<List<ModelProviderEndpointDto>> ToDtosAsync(List<ModelProviderEndpoint> endpoints, CancellationToken cancellationToken)
    {
        var dtos = endpoints.OrderBy(x => x.Name).ToList().Adapt<List<ModelProviderEndpointDto>>();
        foreach (var endpoint in dtos)
        {
            endpoint.Mappings = [.. endpoint.Mappings.OrderByDescending(x => x.Priority)];
        }

        var modelIds = dtos.SelectMany(x => x.Mappings).Select(x => x.ModelId).Distinct().ToList();
        using var _ = modelRepository.BeginAsNoTracking();
        var models = await modelRepository.ToListAsync(
            modelRepository.GetQueryable().Where(x => modelIds.Contains(x.Id)).Select(x => new { x.Id, x.DisplayName }),
            cancellationToken);
        var displayNames = models.ToDictionary(x => x.Id, x => x.DisplayName);
        foreach (var endpoint in dtos)
        {
            foreach (var mapping in endpoint.Mappings)
            {
                mapping.ModelDisplayName = displayNames.GetValueOrDefault(mapping.ModelId, string.Empty);
            }
        }

        return dtos;
    }
}
