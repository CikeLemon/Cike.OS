using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Queries;
using Cike.OS.Domain.Entities;

namespace Cike.OS.Application.ModelProviders;

public class ModelProviderQueryHandler(
    IReadOnlyRepository<ModelProvider, long> providerRepository,
    IReadOnlyRepository<ModelEntity, long> modelRepository)
{
    [LocalEventHandler]
    public async Task GetListAsync(GetModelProviderListQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = providerRepository.BeginAsNoTracking();

        var providers = await providerRepository.ToListAsync(
            providerRepository.WithDetails(x => x.Mappings).OrderBy(x => x.Name),
            cancellationToken);
        query.Result = await ToDtosAsync(providers, cancellationToken);
    }

    [LocalEventHandler]
    public async Task GetAsync(GetModelProviderQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = providerRepository.BeginAsNoTracking();

        var providers = await providerRepository.ToListAsync(
            providerRepository.WithDetails(x => x.Mappings).Where(x => x.Id == query.Id),
            cancellationToken);
        query.Result = providers.Count == 0 ? null : (await ToDtosAsync(providers, cancellationToken))[0];
    }

    /// <summary>
    /// 设置是值对象直接作为契约带出（同工具域先例），Mapster 无法实例化抽象的多态基类，故手写映射；
    /// 映射里的模型名称一次性批量取回填，不逐行查库。
    /// </summary>
    private async Task<List<ModelProviderDto>> ToDtosAsync(List<ModelProvider> providers, CancellationToken cancellationToken)
    {
        var modelIds = providers.SelectMany(x => x.Mappings).Select(x => x.ModelId).Distinct().ToList();
        var models = await modelRepository.ToListAsync(
            modelRepository.GetQueryable().Where(x => modelIds.Contains(x.Id)).Select(x => new { x.Id, x.DisplayName }),
            cancellationToken);
        var modelDisplayNames = models.ToDictionary(x => x.Id, x => x.DisplayName);

        return providers.Select(provider => new ModelProviderDto
        {
            Id = provider.Id,
            CreatedAt = provider.CreatedAt,
            CreatedBy = provider.CreatedBy,
            UpdatedAt = provider.UpdatedAt,
            UpdatedBy = provider.UpdatedBy,
            Name = provider.Name,
            Setting = provider.Setting,
            // 分数高的排前面，与路由取用顺序一致
            Mappings = provider.Mappings
                .OrderByDescending(x => x.Priority)
                .Select(mapping => new ModelProviderMappingDto
                {
                    ModelId = mapping.ModelId,
                    ModelDisplayName = modelDisplayNames.GetValueOrDefault(mapping.ModelId, string.Empty),
                    Priority = mapping.Priority,
                    ProviderModelName = mapping.ProviderModelName,
                })
                .ToList(),
        }).ToList();
    }
}
