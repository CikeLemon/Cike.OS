using Cike.Cqrs.Queries;
using Cike.Contracts.EntityDtos;
using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;

namespace Cike.OS.Application.UserToolConfigurations.Queries;

/// <summary>分页参数打包为 PagedAndSortedResultRequest 对象零组装直传；只有一个筛选字段，平铺即可。</summary>
public record GetUserToolConfigurationPagedListQuery(string? Keyword, PagedAndSortedResultRequest Paging) : Query<PagedResultDto<UserToolConfigurationItemDto>>;
