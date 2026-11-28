using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.EndpointFilters;
using Cike.Contracts.EntityDtos;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.Tags.Dtos;
using Cike.OS.Application.Tags.Commands;
using Cike.OS.Application.Tags.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cike.OS.Service.Open.Services;

/// <summary>标签端点：List 给标签管理页、模型表单的标签选择器，GetById 给编辑表单回填，PagedList 给标签管理页的分页列表，其余为写侧用例。</summary>
[AutoValidation]
public class TagService : MinimalApiServiceBase
{
    public async Task<Ok<List<TagItemDto>>> GetListAsync(
        [FromServices] ILocalEventBus eventBus,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTagListQuery();
        await eventBus.PublishAsync(query, cancellationToken);
        return TypedResults.Ok(query.Result ?? []);
    }

    public async Task<Ok<PagedResultDto<TagItemDto>>> GetPagedListAsync(
        [FromServices] ILocalEventBus eventBus,
        string? keyword,
        [AsParameters] PagedAndSortedResultRequest paging,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTagPagedListQuery(keyword, paging);
        await eventBus.PublishAsync(query, cancellationToken);
        return TypedResults.Ok(query.Result ?? new PagedResultDto<TagItemDto>(0, []));
    }

    public async Task<Results<Ok<TagDetailDto>, NotFound>> GetAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTagDetailQuery(id);
        await eventBus.PublishAsync(query, cancellationToken);
        return query.Result == null ? TypedResults.NotFound() : TypedResults.Ok(query.Result);
    }

    public async Task<Ok<long>> CreateAsync(
        [FromServices] ILocalEventBus eventBus,
        CreateTagDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateTagCommand(dto);
        await eventBus.PublishAsync(command, cancellationToken);
        return TypedResults.Ok(command.Id);
    }

    public async Task<Ok> UpdateAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        UpdateTagDto dto,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new UpdateTagCommand(id, dto), cancellationToken);
        return TypedResults.Ok();
    }

    public async Task<Ok> DeleteAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new DeleteTagCommand(id), cancellationToken);
        return TypedResults.Ok();
    }
}
