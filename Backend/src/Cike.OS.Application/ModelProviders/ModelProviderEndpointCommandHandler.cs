using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Commands;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Repositories;

namespace Cike.OS.Application.ModelProviders;

/// <summary>
/// 接入点用例：接入点是聚合内子实体，没有自己的聚合根——加载提供方（连同接入点与映射），在聚合根的意图方法上
/// 添加 / 修改 / 移除。提供商映射随接入点整体同步，没有独立用例。
/// </summary>
public class ModelProviderEndpointCommandHandler(
    IModelProviderRepository providerRepository,
    IReadOnlyRepository<ModelEntity, long> modelRepository)
{
    [LocalEventHandler]
    public async Task CreateAsync(CreateModelProviderEndpointCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        var provider = await providerRepository.FindWithEndpointsAsync(command.ProviderId, cancellationToken: cancellationToken)
            ?? throw new UserFriendlyException("模型提供方不存在。");
        await ValidateMappingsAsync(dto.Mappings, cancellationToken);

        var endpoint = provider.AddEndpoint(dto.Protocol, dto.Name, dto.BaseUrl, dto.ApiKey, dto.Setting, ToMappings(dto.Mappings));
        await providerRepository.UpdateAsync(provider, cancellationToken: cancellationToken);
        command.Id = endpoint.Id;
    }

    [LocalEventHandler]
    public async Task UpdateAsync(UpdateModelProviderEndpointCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        // 必须连同接入点与映射加载：接入点重名对着名下现有接入点断言，被移除的映射也要处于跟踪状态才会软删
        var provider = await providerRepository.FindWithEndpointsAsync(command.ProviderId, cancellationToken: cancellationToken)
            ?? throw new UserFriendlyException("模型提供方不存在。");
        await ValidateMappingsAsync(dto.Mappings, cancellationToken);

        provider.UpdateEndpoint(command.EndpointId, dto.Name, dto.BaseUrl, dto.ApiKey, dto.Setting, ToMappings(dto.Mappings));
        await providerRepository.UpdateAsync(provider, cancellationToken: cancellationToken);
    }

    [LocalEventHandler]
    public async Task DeleteAsync(DeleteModelProviderEndpointCommand command, CancellationToken cancellationToken = default)
    {
        // 接入点与它的映射随聚合一并删除：子实体必须先加载跟踪，移除后才会跟着软删
        var provider = await providerRepository.FindWithEndpointsAsync(command.ProviderId, cancellationToken: cancellationToken)
            ?? throw new UserFriendlyException("模型提供方不存在。");

        provider.RemoveEndpoint(command.EndpointId);
        await providerRepository.UpdateAsync(provider, cancellationToken: cancellationToken);
    }

    /// <summary>模型是平台目录（无租户过滤）：跨请求里全部映射一次 COUNT 核对去重后的引用数。</summary>
    private async Task ValidateMappingsAsync(List<SaveModelProviderMappingDto> mappings, CancellationToken cancellationToken)
    {
        var modelIds = mappings.Select(x => x.ModelId).Distinct().ToList();
        if (modelIds.Count > 0
            && await modelRepository.GetCountAsync(x => modelIds.Contains(x.Id), cancellationToken) != modelIds.Count)
        {
            throw new UserFriendlyException("部分模型不存在，请刷新模型列表后再提交。");
        }
    }

    private static IEnumerable<ModelProviderEndpoint.MappingInput> ToMappings(List<SaveModelProviderMappingDto> mappings) =>
        mappings.Select(x => new ModelProviderEndpoint.MappingInput(x.ModelId, x.Priority, x.ProviderModelName));
}
