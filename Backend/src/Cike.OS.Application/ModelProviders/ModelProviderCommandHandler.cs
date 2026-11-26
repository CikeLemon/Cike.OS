using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Commands;
using Cike.OS.Domain.Entities;

namespace Cike.OS.Application.ModelProviders;

public class ModelProviderCommandHandler(
    IRepository<ModelProvider, long> providerRepository,
    IReadOnlyRepository<ModelEntity, long> modelRepository)
{
    [LocalEventHandler]
    public async Task CreateAsync(CreateModelProviderCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        await ValidateAsync(dto.Name, dto.Mappings, null, cancellationToken);

        var provider = new ModelProvider
        {
            Name = dto.Name,
            Setting = dto.Setting,
        };
        provider.SyncMappings(dto.Mappings.Select(x => (x.ModelId, x.Priority, x.ProviderModelName)));
        await providerRepository.InsertAsync(provider, cancellationToken: cancellationToken);
        command.Id = provider.Id;
    }

    [LocalEventHandler]
    public async Task UpdateAsync(UpdateModelProviderCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        var provider = await GetWithMappingsAsync(command.Id, cancellationToken);

        await ValidateAsync(dto.Name, dto.Mappings, provider.Id, cancellationToken);

        // 提供商类型创建后不可变：要换类型只能删除重建
        if (dto.Setting.Type != provider.Setting.Type)
        {
            throw new UserFriendlyException("提供商类型创建后不可修改，如需更换请删除后重建。");
        }

        provider.Name = dto.Name;
        provider.Setting = dto.Setting;
        provider.SyncMappings(dto.Mappings.Select(x => (x.ModelId, x.Priority, x.ProviderModelName)));
        await providerRepository.UpdateAsync(provider, cancellationToken: cancellationToken);
    }

    [LocalEventHandler]
    public async Task DeleteAsync(DeleteModelProviderCommand command, CancellationToken cancellationToken = default)
    {
        var provider = await GetWithMappingsAsync(command.Id, cancellationToken);

        // 名下映射随聚合一并删除：子实体必须先加载跟踪，移除后才会跟着软删
        provider.SyncMappings([]);
        await providerRepository.DeleteAsync(provider, cancellationToken: cancellationToken);
    }

    /// <summary>映射是聚合内子实体，改聚合时必须连同集合一起加载（跟踪）。</summary>
    private async Task<ModelProvider> GetWithMappingsAsync(long id, CancellationToken cancellationToken)
    {
        var providers = await providerRepository.ToListAsync(
            providerRepository.WithDetails(x => x.Mappings).Where(x => x.Id == id),
            cancellationToken);
        return providers[0];
    }

    /// <summary>
    /// 创建 / 更新共用的数据校验：名称同租户内唯一（excludeId 排除自身），映射的模型必须都在平台目录里。
    /// 软删过滤生效，软删后同名可重建；并发窗口按 ADR 0001 接受。
    /// </summary>
    private async Task ValidateAsync(string name, List<SaveModelProviderMappingDto> mappings, long? excludeId, CancellationToken cancellationToken)
    {
        var count = await providerRepository.GetCountAsync(x => x.Name == name && x.Id != excludeId, cancellationToken);
        if (count > 0)
        {
            throw new UserFriendlyException($"模型提供方「{name}」已存在。");
        }

        // 模型是平台目录（无租户过滤）：一次 COUNT 核对去重后的引用数
        var modelIds = mappings.Select(x => x.ModelId).Distinct().ToList();
        if (modelIds.Count > 0
            && await modelRepository.GetCountAsync(x => modelIds.Contains(x.Id), cancellationToken) != modelIds.Count)
        {
            throw new UserFriendlyException("部分模型不存在，请刷新模型列表后再提交。");
        }
    }
}
