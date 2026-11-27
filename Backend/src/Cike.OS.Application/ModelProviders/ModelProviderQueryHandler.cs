using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Queries;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Repositories;
using Mapster;

namespace Cike.OS.Application.ModelProviders;

/// <summary>
/// 模型提供方查询：只管提供方自己的名称，接入点经它自己的列表查询读取。
/// 列表不加载聚合，只做投影；详情只读聚合根本身。
/// </summary>
public class ModelProviderQueryHandler(
    IModelProviderRepository providerRepository,
    IReadOnlyRepository<ModelProviderEndpoint, long> endpointRepository,
    IReadOnlyRepository<ModelProviderMapping, long> mappingRepository,
    IReadOnlyRepository<ModelEntity, long> modelRepository)
{
    /// <summary>
    /// 列表：界面只要名称 / 协议 / 支持的模型 / 接入点数 / 创建时间 / 更新时间，所以不加载聚合——
    /// 分四次投影查询，每次只取需要的列，全程不跟踪，再在内存里拼成每行。
    /// </summary>
    [LocalEventHandler]
    public async Task GetListAsync(GetModelProviderListQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = providerRepository.BeginAsNoTracking();
        using var __ = endpointRepository.BeginAsNoTracking();
        using var ___ = mappingRepository.BeginAsNoTracking();
        using var ____ = modelRepository.BeginAsNoTracking();

        var providers = await providerRepository.ToListAsync(
            providerRepository.GetQueryable()
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.CreatedAt, x.CreatedBy, x.UpdatedAt, x.UpdatedBy }),
            cancellationToken);
        var providerIds = providers.Select(x => x.Id).ToList();

        var endpoints = await endpointRepository.ToListAsync(
            endpointRepository.GetQueryable()
                .Where(x => providerIds.Contains(x.ModelProviderId))
                .Select(x => new { x.Id, x.ModelProviderId, x.Protocol }),
            cancellationToken);
        var endpointIds = endpoints.Select(x => x.Id).ToList();

        var mappings = await mappingRepository.ToListAsync(
            mappingRepository.GetQueryable()
                .Where(x => endpointIds.Contains(x.ModelProviderEndpointId))
                .Select(x => new { x.ModelProviderEndpointId, x.ModelId, x.Priority }),
            cancellationToken);
        var modelIds = mappings.Select(x => x.ModelId).Distinct().ToList();

        var models = await modelRepository.ToListAsync(
            modelRepository.GetQueryable().Where(x => modelIds.Contains(x.Id)).Select(x => new { x.Id, x.DisplayName }),
            cancellationToken);
        var displayNames = models.ToDictionary(x => x.Id, x => x.DisplayName);

        var endpointsByProvider = endpoints.ToLookup(x => x.ModelProviderId);
        var mappingsByEndpoint = mappings.ToLookup(x => x.ModelProviderEndpointId);

        query.Result = providers.Select(provider =>
        {
            var ownEndpoints = endpointsByProvider[provider.Id].ToList();
            return new ModelProviderListDto
            {
                Id = provider.Id,
                CreatedAt = provider.CreatedAt,
                CreatedBy = provider.CreatedBy,
                UpdatedAt = provider.UpdatedAt,
                UpdatedBy = provider.UpdatedBy,
                Name = provider.Name,
                Protocols = ownEndpoints.Select(x => x.Protocol).Distinct().OrderBy(x => (int)x).ToList(),
                // 分数高的排前面；同一个模型挂在多个接入点下时只出现一次
                ModelDisplayNames = ownEndpoints
                    .SelectMany(endpoint => mappingsByEndpoint[endpoint.Id])
                    .OrderByDescending(x => x.Priority)
                    .Select(x => displayNames.GetValueOrDefault(x.ModelId, string.Empty))
                    .Where(name => name != string.Empty)
                    .Distinct()
                    .ToList(),
                EndpointCount = ownEndpoints.Count,
            };
        }).ToList();
    }

    /// <summary>详情：实体 → DTO 交给 Mapster（基类继承的 Id / 审计字段按名字自动带上）。</summary>
    [LocalEventHandler]
    public async Task GetAsync(GetModelProviderQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = providerRepository.BeginAsNoTracking();
        var provider = await providerRepository.FindAsync(query.Id, cancellationToken);
        query.Result = provider?.Adapt<ModelProviderDto>();
    }
}
