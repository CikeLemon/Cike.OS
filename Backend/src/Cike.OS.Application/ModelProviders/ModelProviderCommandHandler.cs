using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Commands;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Repositories;

namespace Cike.OS.Application.ModelProviders;

/// <summary>模型提供方用例：只管提供方自己的名称；接入点经它自己的用例添加 / 修改 / 移除。</summary>
public class ModelProviderCommandHandler(IModelProviderRepository providerRepository)
{
    [LocalEventHandler]
    public async Task CreateAsync(CreateModelProviderCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        await ValidateAsync(dto.Name, null, cancellationToken);

        // 提供方本身只有名称：接入点（连接 + 映射）建好后再在详情页里逐个添加
        var provider = ModelProvider.Create(dto.Name);
        await providerRepository.InsertAsync(provider, cancellationToken: cancellationToken);
        command.Id = provider.Id;
    }

    [LocalEventHandler]
    public async Task UpdateAsync(UpdateModelProviderCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        var provider = await providerRepository.FindAsync(command.Id, cancellationToken: cancellationToken)
            ?? throw new UserFriendlyException("模型提供方不存在。");
        await ValidateAsync(dto.Name, provider.Id, cancellationToken);

        provider.Rename(dto.Name);
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
    /// 创建 / 更新共用的数据校验：名称同租户内唯一（excludeId 排除自身）。软删过滤生效，软删后同名可重建；
    /// 并发窗口按 ADR 0001 接受。
    /// </summary>
    private async Task ValidateAsync(string name, long? excludeId, CancellationToken cancellationToken)
    {
        var count = await providerRepository.GetCountAsync(x => x.Name == name && x.Id != excludeId, cancellationToken);
        if (count > 0)
        {
            throw new UserFriendlyException($"模型提供方「{name}」已存在。");
        }
    }
}
