using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Queries;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Repositories;
using Mapster;

namespace Cike.OS.Application.ModelProviders;

/// <summary>接入点列表：单个聚合的只读读取，走自定义仓储连同两层子集合加载；实体 → DTO 交给 Mapster，手写的只有排序与模型显示名称回填。</summary>
public class ModelProviderEndpointQueryHandler(
    IModelProviderRepository providerRepository,
    IReadOnlyRepository<ModelEntity, long> modelRepository)
{
    [LocalEventHandler]
    public async Task GetListAsync(GetModelProviderEndpointListQuery query, CancellationToken cancellationToken = default)
    {
        var provider = await providerRepository.FindWithEndpointsAsync(query.ProviderId, asNoTracking: true, cancellationToken);
        if (provider == null)
        {
            query.Result = null;
            return;
        }

        // 接入点按名称排序（多态设置两边声明类型相同，Mapster 直接引用带出），接入点下的映射按分数降序（与路由取用顺序一致）
        var sorted = provider.Endpoints.OrderBy(x => x.Name).ToList().Adapt<List<ModelProviderEndpointDto>>();
        foreach (var endpoint in sorted)
        {
            endpoint.Mappings = [.. endpoint.Mappings.OrderByDescending(x => x.Priority)];
        }

        // 模型显示名称一次性批量取回填，不逐行查库
        var modelIds = sorted.SelectMany(x => x.Mappings).Select(x => x.ModelId).Distinct().ToList();
        using var _ = modelRepository.BeginAsNoTracking();
        var models = await modelRepository.ToListAsync(
            modelRepository.GetQueryable().Where(x => modelIds.Contains(x.Id)).Select(x => new { x.Id, x.DisplayName }),
            cancellationToken);
        var displayNames = models.ToDictionary(x => x.Id, x => x.DisplayName);
        foreach (var endpoint in sorted)
        {
            foreach (var mapping in endpoint.Mappings)
            {
                mapping.ModelDisplayName = displayNames.GetValueOrDefault(mapping.ModelId, string.Empty);
            }
        }

        query.Result = sorted;
    }
}
