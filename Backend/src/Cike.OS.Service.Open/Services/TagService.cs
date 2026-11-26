using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.EndpointFilters;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.Tags.Dtos;
using Cike.OS.Application.Tags.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cike.OS.Service.Open.Services;

/// <summary>标签只读列表端点：为模型表单的标签多选提供数据；写侧用例另立提案。</summary>
[AutoValidation]
public class TagService : MinimalApiServiceBase
{
    public async Task<Ok<List<TagDto>>> GetListAsync(
        [FromServices] ILocalEventBus eventBus,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTagListQuery();
        await eventBus.PublishAsync(query, cancellationToken);
        return TypedResults.Ok(query.Result ?? []);
    }
}
