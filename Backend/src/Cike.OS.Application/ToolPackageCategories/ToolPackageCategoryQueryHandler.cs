using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;
using Cike.OS.Application.ToolPackageCategories.Queries;
using Cike.OS.Domain.Entities;
using Mapster;

namespace Cike.OS.Application.ToolPackageCategories;

public class ToolPackageCategoryQueryHandler(IReadOnlyRepository<ToolPackageCategory, long> categoryRepository)
{
    [LocalEventHandler]
    public async Task GetListAsync(GetToolPackageCategoryListQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = categoryRepository.BeginAsNoTracking();

        var categories = await categoryRepository.ToListAsync(
            categoryRepository.GetQueryable().OrderBy(x => x.Name),
            cancellationToken);
        query.Result = categories.Adapt<List<ToolPackageCategoryItemDto>>();
    }

    [LocalEventHandler]
    public async Task GetAsync(GetToolPackageCategoryDetailQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = categoryRepository.BeginAsNoTracking();

        var category = await categoryRepository.FindAsync(query.Id, cancellationToken);
        query.Result = category?.Adapt<ToolPackageCategoryDetailDto>();
    }
}
