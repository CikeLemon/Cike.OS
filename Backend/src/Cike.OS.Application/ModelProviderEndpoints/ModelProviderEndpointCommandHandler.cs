using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;
using Cike.OS.Application.ModelProviderEndpoints.Commands;
using Cike.OS.Domain.Entities;

namespace Cike.OS.Application.ModelProviders;

/// <summary>
/// 接入点用例：接入点是独立的聚合，提供商映射是它聚合内的子实体、随它整体同步，没有独立用例。
/// 写之前一律连同映射加载：映射要处于跟踪状态，移除后才会跟着软删。
/// </summary>
public class ModelProviderEndpointCommandHandler(
    IRepository<ModelProviderEndpoint, long> endpointRepository,
    IReadOnlyRepository<ModelProvider, long> providerRepository,
    IReadOnlyRepository<ModelEntity, long> modelRepository)
{
    [LocalEventHandler]
    public async Task CreateAsync(CreateModelProviderEndpointCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        await ValidateAsync(dto.ModelProviderId, dto.Name, dto.Mappings, null, cancellationToken);

        var endpoint = ModelProviderEndpoint.Create(
            dto.ModelProviderId, dto.Protocol, dto.Name, dto.BaseUrl, dto.ApiKey, dto.Setting);
        endpoint.SyncMappings(ToMappings(dto.Mappings));
        await endpointRepository.InsertAsync(endpoint, cancellationToken: cancellationToken);
        command.Id = endpoint.Id;
    }

    [LocalEventHandler]
    public async Task UpdateAsync(UpdateModelProviderEndpointCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        var endpoint = await FindWithMappingsAsync(command.Id, cancellationToken)
            ?? throw new UserFriendlyException("接入点不存在。");
        await ValidateAsync(endpoint.ModelProviderId, dto.Name, dto.Mappings, endpoint.Id, cancellationToken);

        endpoint.Update(dto.Name, dto.BaseUrl, dto.ApiKey, dto.Setting);
        endpoint.SyncMappings(ToMappings(dto.Mappings));
        await endpointRepository.UpdateAsync(endpoint, cancellationToken: cancellationToken);
    }

    [LocalEventHandler]
    public async Task DeleteAsync(DeleteModelProviderEndpointCommand command, CancellationToken cancellationToken = default)
    {
        var endpoint = await FindWithMappingsAsync(command.Id, cancellationToken)
            ?? throw new UserFriendlyException("接入点不存在。");

        await endpointRepository.DeleteAsync(endpoint, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 创建 / 更新共用的数据校验：所属提供方必须存在；接入点名称在该提供方名下互不重复（excludeId 排除自身）；
    /// 映射引用的模型必须都在平台目录里。软删过滤生效，接入点删掉后同名可重建；并发窗口按 ADR 0001 接受。
    /// </summary>
    private async Task ValidateAsync(
        long modelProviderId,
        string name,
        List<SaveModelProviderMappingDto> mappings,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        if (await providerRepository.GetCountAsync(x => x.Id == modelProviderId, cancellationToken) == 0)
        {
            throw new UserFriendlyException("模型提供方不存在。");
        }

        if (await endpointRepository.GetCountAsync(
                x => x.ModelProviderId == modelProviderId && x.Name == name && x.Id != excludeId, cancellationToken) > 0)
        {
            throw new UserFriendlyException($"接入点名称「{name}」已存在。");
        }

        // 模型是平台目录（无租户过滤）：一次 COUNT 核对去重后的引用数
        var modelIds = mappings.Select(x => x.ModelId).Distinct().ToList();
        if (modelIds.Count > 0
            && await modelRepository.GetCountAsync(x => modelIds.Contains(x.Id), cancellationToken) != modelIds.Count)
        {
            throw new UserFriendlyException("部分模型不存在，请刷新模型列表后再提交。");
        }
    }

    /// <summary>连同映射加载一个接入点：映射是这个聚合里唯一的一级导航，通用仓储的预加载就够。</summary>
    private async Task<ModelProviderEndpoint?> FindWithMappingsAsync(long id, CancellationToken cancellationToken)
    {
        var endpoints = await endpointRepository.ToListAsync(
            endpointRepository.WithDetails(x => x.Mappings).Where(x => x.Id == id), cancellationToken);
        return endpoints.FirstOrDefault();
    }

    private static IEnumerable<ModelProviderEndpoint.MappingInput> ToMappings(List<SaveModelProviderMappingDto> mappings) =>
        mappings.Select(x => new ModelProviderEndpoint.MappingInput(x.ModelId, x.Priority, x.ProviderModelName));
}
