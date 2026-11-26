using Cike.Data;
using Cike.Data.DataFilters;
using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Models.Commands;
using Cike.OS.Domain.Entities;

namespace Cike.OS.Application.Models;

public class ModelCommandHandler(
    IRepository<ModelEntity, long> modelRepository,
    IReadOnlyRepository<Tag, long> tagRepository,
    IReadOnlyRepository<ModelProviderMapping, long> mappingRepository,
    IDataFilter dataFilter)
{
    [LocalEventHandler]
    public async Task CreateAsync(CreateModelCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        await ValidateAsync(dto.Name, dto.DisplayName, dto.TagIds, null, cancellationToken);

        var model = ModelEntity.Create(dto.Name, dto.DisplayName, dto.Description, dto.TagIds);
        await modelRepository.InsertAsync(model, cancellationToken: cancellationToken);
        command.Id = model.Id;
    }

    [LocalEventHandler]
    public async Task UpdateAsync(UpdateModelCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        var model = await modelRepository.GetAsync(command.Id, cancellationToken);

        // 模型名称创建后不可变：传入的是自身名称，而自身已被 excludeId 排除，这一项不会误报
        await ValidateAsync(model.Name, dto.DisplayName, dto.TagIds, model.Id, cancellationToken);

        model.Update(dto.DisplayName, dto.Description, dto.TagIds);
        await modelRepository.UpdateAsync(model, cancellationToken: cancellationToken);
    }

    [LocalEventHandler]
    public async Task DeleteAsync(DeleteModelCommand command, CancellationToken cancellationToken = default)
    {
        var model = await modelRepository.GetAsync(command.Id, cancellationToken);

        // 模型是平台目录，映射是租户数据：关掉租户过滤器做全库计数，任何租户的映射都会挡住删除
        long mappingCount;
        using (dataFilter.Disable<IMultiTenant>())
        {
            mappingCount = await mappingRepository.GetCountAsync(x => x.ModelId == model.Id, cancellationToken);
        }

        if (mappingCount > 0)
        {
            throw new UserFriendlyException("该模型仍有提供商映射，请先移除全部映射后再删除。");
        }

        await modelRepository.DeleteAsync(model, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 创建 / 更新共用的数据校验：模型名称、显示名称全局唯一（两项合并为一次查询，excludeId 排除自身），标签引用必须存在。
    /// 软删过滤生效，软删后同名可重建；并发窗口按 ADR 0001 接受。
    /// </summary>
    private async Task ValidateAsync(string name, string displayName, List<long>? tagIds, long? excludeId, CancellationToken cancellationToken)
    {
        // 命中标志在 SQL 里比较（沿用列的排序规则），只回传两个布尔而不是整行
        var duplicates = await modelRepository.ToListAsync(
            modelRepository.GetQueryable()
                .Where(x => x.Id != excludeId && (x.Name == name || x.DisplayName == displayName))
                .Select(x => new { NameHit = x.Name == name, DisplayNameHit = x.DisplayName == displayName }),
            cancellationToken);

        if (duplicates.Any(x => x.NameHit))
        {
            throw new UserFriendlyException($"模型名称「{name}」已存在。");
        }

        if (duplicates.Any(x => x.DisplayNameHit))
        {
            throw new UserFriendlyException($"显示名称「{displayName}」已存在。");
        }

        // 标签在另一张表：一次 COUNT 核对去重后的引用数
        var tagIdList = (tagIds ?? []).Distinct().ToList();
        if (tagIdList.Count > 0
            && await tagRepository.GetCountAsync(x => tagIdList.Contains(x.Id), cancellationToken) != tagIdList.Count)
        {
            throw new UserFriendlyException("部分标签不存在，请刷新标签列表后再提交。");
        }
    }
}
