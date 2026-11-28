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

        // 参数定义是 json 列，不能在 SQL 里数个数：Adapt 之后按取回的实体顺序一一对应数
        var items = tools.Adapt<List<ToolItemDto>>();
        foreach (var (item, tool) in items.Zip(tools))
        {
            item.ParameterCount = tool.Parameters.Count;
        }
        query.Result = items;
    }

    [LocalEventHandler]
    public async Task GetAsync(GetToolDetailQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = toolRepository.BeginAsNoTracking();

        var tool = await toolRepository.FindAsync(query.Id, cancellationToken);
        query.Result = tool?.Adapt<ToolDetailDto>();
    }
}
