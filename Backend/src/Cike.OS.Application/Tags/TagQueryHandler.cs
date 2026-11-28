using Cike.Data.Domain.Repositories;
using Cike.Contracts.EntityDtos;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.Tags.Dtos;
using Cike.OS.Application.Tags.Queries;
using Cike.OS.Domain.Entities;
using Mapster;
using System.Linq.Expressions;

namespace Cike.OS.Application.Tags;

public class TagQueryHandler(IReadOnlyRepository<Tag, long> tagRepository)
{
    [LocalEventHandler]
    public async Task GetListAsync(GetTagListQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = tagRepository.BeginAsNoTracking();

        var tags = await tagRepository.ToListAsync(
            tagRepository.GetQueryable().OrderBy(x => x.Key),
            cancellationToken);
        query.Result = tags.Adapt<List<TagItemDto>>();
    }

    /// <summary>详情：编辑表单回填用。</summary>
    [LocalEventHandler]
    public async Task GetDetailAsync(GetTagDetailQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = tagRepository.BeginAsNoTracking();

        var tag = await tagRepository.FindAsync(query.Id, cancellationToken);
        query.Result = tag?.Adapt<TagDetailDto>();
    }

    [LocalEventHandler]
    public async Task GetPagedListAsync(GetTagPagedListQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = tagRepository.BeginAsNoTracking();

        Expression<Func<Tag, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            predicate = x => x.Key.Contains(query.Keyword) || x.Name.Contains(query.Keyword);
        }

        // Paging 从端点 [AsParameters] 绑定后一路透传：Sorting 由前端传入（Dynamic LINQ 字符串，支持多字段）
        var (total, tags) = await tagRepository.GetPagedListAsync(query.Paging, predicate, cancellationToken);
        query.Result = new PagedResultDto<TagItemDto>(total, tags.Adapt<List<TagItemDto>>());
    }
}
