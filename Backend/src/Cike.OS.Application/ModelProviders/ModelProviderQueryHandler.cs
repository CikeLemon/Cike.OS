using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Queries;
using Cike.OS.Domain.Entities;
using Mapster;

namespace Cike.OS.Application.ModelProviders;

/// <summary>
/// 模型提供方查询：列表只做投影（不加载聚合），详情连同模型一起加载后交给 Mapster。
/// </summary>
public class ModelProviderQueryHandler(
    IReadOnlyRepository<ModelProvider, long> providerRepository,
    IReadOnlyRepository<ModelEntity, long> modelRepository)
{
    /// <summary>
    /// 列表：界面只要名称 / 协议 / 模型名称 / 创建时间 / 更新时间，所以不加载聚合——
    /// 分两次投影查询，每次只取需要的列，全程不跟踪；投影结果 → 列表 DTO 交给 Mapster，模型名称在 Adapt 之后回填。
    /// </summary>
    [LocalEventHandler]
    public async Task GetListAsync(GetModelProviderListQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = providerRepository.BeginAsNoTracking();
        using var __ = modelRepository.BeginAsNoTracking();

        var providers = await providerRepository.ToListAsync(
            providerRepository.GetQueryable()
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name, x.Protocol, x.CreatedAt, x.CreatedBy, x.UpdatedAt, x.UpdatedBy }),
            cancellationToken);
        var providerIds = providers.Select(x => x.Id).ToList();

        var models = await modelRepository.ToListAsync(
            modelRepository.GetQueryable()
                .Where(x => providerIds.Contains(x.ModelProviderId))
                .Select(x => new { x.ModelProviderId, x.Name }),
            cancellationToken);
        var modelsByProvider = models.ToLookup(x => x.ModelProviderId);

        var items = providers.Adapt<List<ModelProviderItemDto>>();
        foreach (var item in items)
        {
            item.ModelNames = modelsByProvider[item.Id].Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal).ToList();
        }

        query.Result = items;
    }

    /// <summary>详情：连同模型只读加载，实体 → DTO 交给 Mapster（基类继承的 Id / 审计字段按名字自动带上），模型按名称排序。</summary>
    [LocalEventHandler]
    public async Task GetAsync(GetModelProviderDetailQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = providerRepository.BeginAsNoTracking();

        var providers = await providerRepository.ToListAsync(
            providerRepository.WithDetails(x => x.Models).Where(x => x.Id == query.Id), cancellationToken);
        var provider = providers.FirstOrDefault();
        if (provider == null)
        {
            query.Result = null;
            return;
        }

        var dto = provider.Adapt<ModelProviderDetailDto>();
        dto.Models = dto.Models.OrderBy(x => x.Name, StringComparer.Ordinal).ToList();
        query.Result = dto;
    }
}
