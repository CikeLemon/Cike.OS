using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.EndpointFilters;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.Tools.Dtos;
using Cike.OS.Application.Tools.Commands;
using Cike.OS.Application.Tools.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cike.OS.Service.Open.Services;

[AutoValidation]
public class ToolService : MinimalApiServiceBase
{
    public async Task<Ok<List<ToolDto>>> GetListAsync(
        [FromServices] ILocalEventBus eventBus,
        long toolPackageId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetToolListQuery(toolPackageId);
        await eventBus.PublishAsync(query, cancellationToken);
        return TypedResults.Ok(query.Result ?? []);
    }

    public async Task<Results<Ok<ToolDto>, NotFound>> GetAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetToolQuery(id);
        await eventBus.PublishAsync(query, cancellationToken);
        return query.Result == null ? TypedResults.NotFound() : TypedResults.Ok(query.Result);
    }

    public async Task<Ok<long>> CreateAsync(
        [FromServices] ILocalEventBus eventBus,
        CreateToolDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateToolCommand(dto);
        await eventBus.PublishAsync(command, cancellationToken);
        return TypedResults.Ok(command.Id);
    }

    public async Task<Ok> UpdateAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        UpdateToolDto dto,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new UpdateToolCommand(id, dto), cancellationToken);
        return TypedResults.Ok();
    }

    public async Task<Ok> DeleteAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new DeleteToolCommand(id), cancellationToken);
        return TypedResults.Ok();
    }
}
