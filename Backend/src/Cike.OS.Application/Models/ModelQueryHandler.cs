using Cike.Data.Domain.Repositories;
using Cike.Contracts.EntityDtos;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.Models.Dtos;
using Cike.OS.Application.Models.Queries;
using Cike.OS.Domain.Entities;
using Mapster;
using System.Linq.Expressions;

namespace Cike.OS.Application.Models;

public class ModelQueryHandler(
    IReadOnlyRepository<ModelEntity, long> modelRepository,
    IReadOnlyRepository<Tag, long> tagRepository)
{
    [LocalEventHandler]
    public async Task GetPagedListAsync(GetModelPagedListQuery query, CancellationToken cancellationToken = default)
    {
        var dto = query.Dto;
        using var _ = modelRepository.BeginAsNoTracking();

        Expression<Func<ModelEntity, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(dto.Keyword))
        {
            predicate = x => x.Name.Contains(dto.Keyword) || x.DisplayName.Contains(dto.Keyword);
        }

        // dto 本身就是分页排序请求：Sorting 由前端传入（Dynamic LINQ 字符串，支持 "DisplayName asc, Name desc" 这样的多字段）
        var (total, models) = await modelRepository.GetPagedListAsync(dto, predicate, cancellationToken);
        var items = models.Adapt<List<ModelDto>>();

        // 标签删除不清理模型上的引用：页内引用的标签 Id 一次查出仍然存在的，不存在的（已删）不再返回
        var tagIds = items.SelectMany(x => x.TagIds).Distinct().ToList();
        if (tagIds.Count > 0)
        {
            using var tagScope = tagRepository.BeginAsNoTracking();
            var existingTagIds = await tagRepository.ToListAsync(
                tagRepository.GetQueryable().Where(x => tagIds.Contains(x.Id)).Select(x => x.Id),
                cancellationToken);

            foreach (var item in items)
            {
                item.TagIds = item.TagIds.Where(existingTagIds.Contains).ToList();
            }
        }

        query.Result = new PagedResultDto<ModelDto>(total, items);
    }

    [LocalEventHandler]
    public async Task GetAsync(GetModelQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = modelRepository.BeginAsNoTracking();

        var model = await modelRepository.FindAsync(query.Id, cancellationToken);
        query.Result = model?.Adapt<ModelDto>();
    }
}
