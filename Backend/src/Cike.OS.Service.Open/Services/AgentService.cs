using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.EndpointFilters;
using Cike.Contracts.EntityDtos;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.Agents.Dtos;
using Cike.OS.Application.Agents.Commands;
using Cike.OS.Application.Agents.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cike.OS.Service.Open.Services;

/// <summary>Agent 端点：PagedList 给列表页，ToolOptions 给表单第二步的选择列表，其余为单条读写。</summary>
[AutoValidation]
public class AgentService : MinimalApiServiceBase
{
    public async Task<Ok<PagedResultDto<AgentItemDto>>> GetPagedListAsync(
        [FromServices] ILocalEventBus eventBus,
        string? keyword,
        [AsParameters] PagedAndSortedResultRequest paging,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAgentPagedListQuery(keyword, paging);
        await eventBus.PublishAsync(query, cancellationToken);
        return TypedResults.Ok(query.Result ?? new PagedResultDto<AgentItemDto>(0, []));
    }

    public async Task<Ok<List<AgentToolPackageOptionItemDto>>> GetToolOptionsAsync(
        [FromServices] ILocalEventBus eventBus,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAgentToolOptionsQuery();
        await eventBus.PublishAsync(query, cancellationToken);
        return TypedResults.Ok(query.Result ?? []);
    }

    public async Task<Results<Ok<AgentDetailDto>, NotFound>> GetAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAgentDetailQuery(id);
        await eventBus.PublishAsync(query, cancellationToken);
        return query.Result == null ? TypedResults.NotFound() : TypedResults.Ok(query.Result);
    }

    public async Task<Ok<long>> CreateAsync(
        [FromServices] ILocalEventBus eventBus,
        CreateAgentDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateAgentCommand(dto);
        await eventBus.PublishAsync(command, cancellationToken);
        return TypedResults.Ok(command.Id);
    }

    public async Task<Ok> UpdateAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        UpdateAgentDto dto,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new UpdateAgentCommand(id, dto), cancellationToken);
        return TypedResults.Ok();
    }

    public async Task<Ok> DeleteAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new DeleteAgentCommand(id), cancellationToken);
        return TypedResults.Ok();
    }
}
