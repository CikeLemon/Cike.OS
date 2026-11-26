using Cike.Core.Extensions.Expressions;
using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ToolPackages.Dtos;
using Cike.OS.Application.ToolPackages.Queries;
using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Entities;
using Mapster;
using System.Linq.Expressions;

namespace Cike.OS.Application.ToolPackages;

public class ToolPackageQueryHandler(
    IReadOnlyRepository<ToolPackage, long> toolPackageRepository,
    IReadOnlyRepository<ToolPackageCategory, long> categoryRepository)
{
    [LocalEventHandler]
    public async Task GetPagedListAsync(GetToolPackagePagedListQuery query, CancellationToken cancellationToken = default)
    {
        var dto = query.Dto;
        using var _ = toolPackageRepository.BeginAsNoTracking();

        Expression<Func<ToolPackage, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(dto.Keyword))
        {
            predicate = x => x.Name.Contains(dto.Keyword) || x.Code.Contains(dto.Keyword);
        }

        if (dto.ToolPackageCategoryId is { } categoryId)
        {
            predicate = predicate == null
                ? x => x.ToolPackageCategoryId == categoryId
                : predicate.And(x => x.ToolPackageCategoryId == categoryId);
        }

        if (dto.Status is { } status)
        {
            predicate = predicate == null
                ? x => x.Status == status
                : predicate.And(x => x.Status == status);
        }

        // dto 本身就是分页排序请求：Sorting 由前端传入（Dynamic LINQ 字符串，支持多字段）
        var (total, packages) = await toolPackageRepository.GetPagedListAsync(dto, predicate, cancellationToken);

        var categoryIds = packages.Select(x => x.ToolPackageCategoryId).Distinct().ToList();
        var categories = await categoryRepository.GetListAsync(x => categoryIds.Contains(x.Id), "Id asc", cancellationToken);
        var categoryNames = categories.ToDictionary(x => x.Id, x => x.Name);

        var items = packages.Adapt<List<ToolPackageDto>>();
        foreach (var item in items)
        {
            categoryNames.TryGetValue(item.ToolPackageCategoryId, out var name);
            item.ToolPackageCategoryName = name ?? string.Empty;
        }

        query.Result = new PagedResultDto<ToolPackageDto>(total, items);
    }

    [LocalEventHandler]
    public async Task GetAsync(GetToolPackageQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = toolPackageRepository.BeginAsNoTracking();

        var package = await toolPackageRepository.FindAsync(query.Id, cancellationToken);
        if (package == null)
        {
            query.Result = null;
            return;
        }

        var category = await categoryRepository.FindAsync(package.ToolPackageCategoryId, cancellationToken);
        var dto = package.Adapt<ToolPackageDto>();
        dto.ToolPackageCategoryName = category?.Name ?? string.Empty;
        query.Result = dto;
    }
}
