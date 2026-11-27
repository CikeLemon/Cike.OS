using Cike.Cqrs.Queries;
using Cike.Contracts.EntityDtos;
using Cike.OS.Application.Contracts.Tags.Dtos;

namespace Cike.OS.Application.Tags.Queries;

/// <summary>分页参数打包为 PagedAndSortedResultRequest 对象零组装直传仓储；标签只有一个筛选字段，平铺即可。</summary>
public record GetTagPagedListQuery(string? Keyword, PagedAndSortedResultRequest Paging) : Query<PagedResultDto<TagDto>>;
