using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Commands;
using Cike.OS.Domain.Entities;

namespace Cike.OS.Application.ModelProviders;

/// <summary>
/// 模型提供方用例：提供方是一条连接，模型是它聚合内的子实体、随提供方的请求整体同步，没有独立用例。
/// 更新 / 删除前一律连同模型加载：模型要处于跟踪状态，被移除后才会跟着软删。
/// </summary>
public class ModelProviderCommandHandler(
    IRepository<ModelProvider, long> providerRepository,
    IReadOnlyRepository<Tag, long> tagRepository)
{
    [LocalEventHandler]
    public async Task CreateAsync(CreateModelProviderCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        await ValidateAsync(dto.Name, dto.Models, null, cancellationToken);

        var provider = ModelProvider.Create(dto.Name, dto.Protocol, dto.BaseUrl, dto.ApiKey, dto.Setting, ToModels(dto.Models));
        await providerRepository.InsertAsync(provider, cancellationToken: cancellationToken);
        command.Id = provider.Id;
    }

    [LocalEventHandler]
    public async Task UpdateAsync(UpdateModelProviderCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        var provider = await FindWithModelsAsync(command.Id, cancellationToken)
            ?? throw new UserFriendlyException("模型提供方不存在。");
        await ValidateAsync(dto.Name, dto.Models, provider.Id, cancellationToken);

        provider.Update(dto.Name, dto.Protocol, dto.BaseUrl, dto.ApiKey, dto.Setting);
        provider.SyncModels(ToModels(dto.Models));
        await providerRepository.UpdateAsync(provider, cancellationToken: cancellationToken);
    }

    [LocalEventHandler]
    public async Task DeleteAsync(DeleteModelProviderCommand command, CancellationToken cancellationToken = default)
    {
        // 名下的模型随聚合一并软删，没有任何守护
        var provider = await FindWithModelsAsync(command.Id, cancellationToken)
            ?? throw new UserFriendlyException("模型提供方不存在。");

        await providerRepository.DeleteAsync(provider, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 创建 / 更新共用的数据校验：提供方名称同租户内唯一（excludeId 排除自身）；全部模型引用的标签必须都存在。
    /// 软删过滤生效，软删后同名可重建；并发窗口按 ADR 0001 接受。
    /// </summary>
    private async Task ValidateAsync(string name, List<SaveModelProviderModelDto> models, long? excludeId, CancellationToken cancellationToken)
    {
        if (await providerRepository.GetCountAsync(x => x.Name == name && x.Id != excludeId, cancellationToken) > 0)
        {
            throw new UserFriendlyException($"模型提供方「{name}」已存在。");
        }

        // 标签在另一张表（平台级、无租户过滤）：把全部模型的引用去重后一次 COUNT 核对
        var tagIds = models.SelectMany(x => x.TagIds).Distinct().ToList();
        if (tagIds.Count > 0
            && await tagRepository.GetCountAsync(x => tagIds.Contains(x.Id), cancellationToken) != tagIds.Count)
        {
            throw new UserFriendlyException("部分标签不存在，请刷新标签列表后再提交。");
        }
    }

    /// <summary>连同模型加载一个提供方：模型是这个聚合里唯一的一级导航，通用仓储的预加载就够。</summary>
    private async Task<ModelProvider?> FindWithModelsAsync(long id, CancellationToken cancellationToken)
    {
        var providers = await providerRepository.ToListAsync(
            providerRepository.WithDetails(x => x.Models).Where(x => x.Id == id), cancellationToken);
        return providers.FirstOrDefault();
    }

    private static IEnumerable<ModelEntity> ToModels(List<SaveModelProviderModelDto> models) =>
        models.Select(x => ModelEntity.Create(x.Name, x.Description, x.TagIds));
}
