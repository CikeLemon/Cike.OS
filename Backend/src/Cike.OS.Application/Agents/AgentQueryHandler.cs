using Cike.Data.Domain.Repositories;
using Cike.Contracts.EntityDtos;
using Cike.EventBus.Local;
using Cike.OS.Application.Agents.Queries;
using Cike.OS.Application.Contracts.Agents.Dtos;
using Cike.OS.Domain.Entities;
using Mapster;
using System.Linq.Expressions;

namespace Cike.OS.Application.Agents;

/// <summary>
/// Agent 查询：列表只做投影（不加载聚合），详情连同工具一起加载后交给 Mapster，
/// 工具选项列表两次投影后在内存里组装成「工具包 → 工具」两层。
/// </summary>
public class AgentQueryHandler(
    IReadOnlyRepository<AgentEntity, long> agentRepository,
    IReadOnlyRepository<Tool, long> toolRepository,
    IReadOnlyRepository<ToolPackage, long> toolPackageRepository)
{
    [LocalEventHandler]
    public async Task GetPagedListAsync(GetAgentPagedListQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = agentRepository.BeginAsNoTracking();

        Expression<Func<AgentEntity, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            predicate = x => x.Name.Contains(query.Keyword);
        }

        // Paging 从端点 [AsParameters] 绑定后一路透传：Sorting 由前端传入（Dynamic LINQ 字符串，支持多字段）。
        // 仓储的 IQueryable 分页只接受实体查询，投影不能分页，所以这里按先例 TagQueryHandler 整行取回后交给 Mapster
        var (total, agents) = await agentRepository.GetPagedListAsync(query.Paging, predicate, cancellationToken);
        query.Result = new PagedResultDto<AgentItemDto>(total, agents.Adapt<List<AgentItemDto>>());
    }

    /// <summary>详情：连同工具只读加载，实体 → DTO 交给 Mapster（基类继承的 Id / 审计字段按名字自动带上），工具按类型、目标排序。</summary>
    [LocalEventHandler]
    public async Task GetAsync(GetAgentDetailQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = agentRepository.BeginAsNoTracking();

        var agents = await agentRepository.ToListAsync(
            agentRepository.WithDetails(x => x.Tools).Where(x => x.Id == query.Id), cancellationToken);
        var agent = agents.FirstOrDefault();
        if (agent == null)
        {
            query.Result = null;
            return;
        }

        var dto = agent.Adapt<AgentDetailDto>();
        dto.Tools = dto.Tools.OrderBy(x => x.Type).ThenBy(x => x.TargetId).ToList();
        query.Result = dto;
    }

    /// <summary>
    /// 表单第二步的选择列表：工具 / 工具包各一次投影（只取 Id、名称与归属），内存里按「工具包 → 工具」组装，
    /// 只保留有工具的包，各层按名称排序；不限工具包状态。分类只是工具包列表的检索维度，这里不参与。
    /// </summary>
    [LocalEventHandler]
    public async Task GetToolOptionsAsync(GetAgentToolOptionsQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = toolRepository.BeginAsNoTracking();
        using var __ = toolPackageRepository.BeginAsNoTracking();

        // 参数定义是 json 列，不能在 SQL 里数个数，取回后在内存里数
        var tools = await toolRepository.ToListAsync(
            toolRepository.GetQueryable()
                .OrderBy(x => x.Name)
                .Select(x => new { ToolId = x.Id, x.Name, Type = x.Type, x.ToolPackageId, x.Parameters }),
            cancellationToken);
        var packageIds = tools.Select(x => x.ToolPackageId).Distinct().ToList();

        var packages = await toolPackageRepository.ToListAsync(
            toolPackageRepository.GetQueryable()
                .Where(x => packageIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => new { ToolPackageId = x.Id, x.Name }),
            cancellationToken);

        var toolsByPackage = tools.ToLookup(x => x.ToolPackageId);

        query.Result = packages.Select(package =>
        {
            var item = package.Adapt<AgentToolPackageOptionItemDto>();
            item.Tools = toolsByPackage[package.ToolPackageId].Select(tool =>
            {
                var toolItem = tool.Adapt<AgentToolOptionItemDto>();
                toolItem.ParameterCount = tool.Parameters.Count;
                return toolItem;
            }).ToList();
            return item;
        }).ToList();
    }
}
