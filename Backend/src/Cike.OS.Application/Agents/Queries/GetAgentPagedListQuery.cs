using Cike.Cqrs.Queries;
using Cike.Contracts.EntityDtos;
using Cike.OS.Application.Contracts.Agents.Dtos;

namespace Cike.OS.Application.Agents.Queries;

/// <summary>分页参数打包为 PagedAndSortedResultRequest 对象零组装直传仓储；Agent 只有一个筛选字段，平铺即可。</summary>
public record GetAgentPagedListQuery(string? Keyword, PagedAndSortedResultRequest Paging) : Query<PagedResultDto<AgentItemDto>>;
