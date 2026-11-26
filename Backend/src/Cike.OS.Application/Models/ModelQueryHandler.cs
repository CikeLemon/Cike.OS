using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.Contracts.EntityDtos;
using Cike.OS.Application.Contracts.Models.Dtos;
using Cike.OS.Application.Models.Queries;
using Cike.OS.Domain.Entities;
using Mapster;
using System.Linq.Expressions;

namespace Cike.OS.Application.Models;

public class ModelQueryHandler(IReadOnlyRepository<ModelEntity, long> modelRepository)
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
        query.Result = new PagedResultDto<ModelDto>(total, models.Adapt<List<ModelDto>>());
    }

    [LocalEventHandler]
    public async Task GetAsync(GetModelQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = modelRepository.BeginAsNoTracking();

        var model = await modelRepository.FindAsync(query.Id, cancellationToken);
        query.Result = model?.Adapt<ModelDto>();
    }
}
