using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.Tools.Dtos;
using Cike.OS.Application.Tools.Queries;
using Cike.OS.Domain.Entities;

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

        query.Result = tools.Select(ToDto).ToList();
    }

    [LocalEventHandler]
    public async Task GetAsync(GetToolQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = toolRepository.BeginAsNoTracking();

        var tool = await toolRepository.FindAsync(query.Id, cancellationToken);
        query.Result = tool == null ? null : ToDto(tool);
    }

    /// <summary>参数定义与设置都是值对象，直接作为契约字段带出，不做二次映射。</summary>
    private static ToolDto ToDto(Tool tool)
    {
        return new ToolDto
        {
            Id = tool.Id,
            CreatedAt = tool.CreatedAt,
            CreatedBy = tool.CreatedBy,
            UpdatedAt = tool.UpdatedAt,
            UpdatedBy = tool.UpdatedBy,
            Code = tool.Code,
            Name = tool.Name,
            Description = tool.Description,
            Type = tool.Type,
            ToolPackageId = tool.ToolPackageId,
            Parameters = [.. tool.Parameters],
            Setting = tool.Setting,
        };
    }
}
