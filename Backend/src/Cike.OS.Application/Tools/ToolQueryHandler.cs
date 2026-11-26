using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.Tools.Dtos;
using Cike.OS.Application.Tools.Queries;
using Cike.OS.Domain.Entities;
using Mapster;

namespace Cike.OS.Application.Tools;

public class ToolQueryHandler(IReadOnlyRepository<Tool, long> toolRepository)
{
    [LocalEventHandler]
    public async Task GetListAsync(GetToolListQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = toolRepository.BeginAsNoTracking();

        var tools = await toolRepository.ToListAsync(
            toolRepository.GetQueryable()
                .Where(x => x.ToolPackageId == query.ToolPackageId)
                .OrderBy(x => x.Code),
            cancellationToken);

        // 参数定义与设置都是值对象，直接作为契约字段带出（两边声明类型相同，Mapster 原样引用）
        query.Result = tools.Adapt<List<ToolDto>>();
    }

    [LocalEventHandler]
    public async Task GetAsync(GetToolQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = toolRepository.BeginAsNoTracking();

        var tool = await toolRepository.FindAsync(query.Id, cancellationToken);
        query.Result = tool?.Adapt<ToolDto>();
    }
}
