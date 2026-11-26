using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Commands;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Repositories;

namespace Cike.OS.Application.ModelProviders;

/// <summary>
/// 模型提供方用例：接入点与映射都是聚合内子实体，没有独立的用例——随提供方一起加载、修改、保存。
/// </summary>
public class ModelProviderCommandHandler(
    IModelProviderRepository providerRepository,
    IReadOnlyRepository<ModelEntity, long> modelRepository)
{
    [LocalEventHandler]
    public async Task CreateAsync(CreateModelProviderCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        await ValidateAsync(dto.Name, [], null, cancellationToken);

        // 提供方本身只有名称：接入点（连接 + 映射）建好后再在详情页里逐个添加
        var provider = ModelProvider.Create(dto.Name);
        await providerRepository.InsertAsync(provider, cancellationToken: cancellationToken);
        command.Id = provider.Id;
    }

    [LocalEventHandler]
    public async Task UpdateAsync(UpdateModelProviderCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        // 必须连同接入点与映射加载：接入点重名 / 协议不可变要对着名下现有接入点断言，被移除的也要处于跟踪状态才会软删
        var provider = await providerRepository.FindWithEndpointsAsync(command.Id, cancellationToken: cancellationToken)
            ?? throw new UserFriendlyException("模型提供方不存在。");
        await ValidateAsync(dto.Name, dto.Endpoints, provider.Id, cancellationToken);

        provider.Rename(dto.Name);
        provider.SyncEndpoints(dto.Endpoints.Select(x => new ModelProvider.EndpointInput(
            x.Id,
            x.Protocol,
            x.Name,
            x.BaseUrl,
            x.ApiKey,
            x.Setting,
            x.Mappings.Select(mapping => (mapping.ModelId, mapping.Priority, mapping.ProviderModelName)).ToList())));
        await providerRepository.UpdateAsync(provider, cancellationToken: cancellationToken);
    }

    [LocalEventHandler]
    public async Task DeleteAsync(DeleteModelProviderCommand command, CancellationToken cancellationToken = default)
    {
        // 名下的接入点与映射随聚合一并删除：子实体必须先加载跟踪，移除后才会跟着软删
        var provider = await providerRepository.FindWithEndpointsAsync(command.Id, cancellationToken: cancellationToken)
            ?? throw new UserFriendlyException("模型提供方不存在。");
        await providerRepository.DeleteAsync(provider, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 创建 / 更新共用的数据校验：名称同租户内唯一（excludeId 排除自身），各接入点映射引用的模型必须都在平台目录里。
    /// 软删过滤生效，软删后同名可重建；并发窗口按 ADR 0001 接受。
    /// </summary>
    private async Task ValidateAsync(string name, List<SaveModelProviderEndpointDto> endpoints, long? excludeId, CancellationToken cancellationToken)
    {
        var count = await providerRepository.GetCountAsync(x => x.Name == name && x.Id != excludeId, cancellationToken);
        if (count > 0)
        {
            throw new UserFriendlyException($"模型提供方「{name}」已存在。");
        }

        // 模型是平台目录（无租户过滤）：跨全部接入点一次 COUNT 核对去重后的引用数
        var modelIds = endpoints.SelectMany(x => x.Mappings).Select(x => x.ModelId).Distinct().ToList();
        if (modelIds.Count > 0
            && await modelRepository.GetCountAsync(x => modelIds.Contains(x.Id), cancellationToken) != modelIds.Count)
        {
            throw new UserFriendlyException("部分模型不存在，请刷新模型列表后再提交。");
        }
    }
}
