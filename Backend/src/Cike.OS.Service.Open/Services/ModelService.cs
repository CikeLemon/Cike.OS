using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.EndpointFilters;
using Cike.EventBus.Local;
using Cike.Contracts.EntityDtos;
using Cike.OS.Application.Contracts.Models.Dtos;
using Cike.OS.Application.Models.Commands;
using Cike.OS.Application.Models.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cike.OS.Service.Open.Services;

[AutoValidation]
public class ModelService : MinimalApiServiceBase
{
    public async Task<Ok<PagedResultDto<ModelDto>>> GetPagedListAsync(
        [FromServices] ILocalEventBus eventBus,
        [AsParameters] GetModelPagedListDto dto,
        CancellationToken cancellationToken = default)
    {
        var query = new GetModelPagedListQuery(dto);
        await eventBus.PublishAsync(query, cancellationToken);
        return TypedResults.Ok(query.Result ?? new PagedResultDto<ModelDto>(0, []));
    }

    public async Task<Results<Ok<ModelDto>, NotFound>> GetAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetModelQuery(id);
        await eventBus.PublishAsync(query, cancellationToken);
        return query.Result == null ? TypedResults.NotFound() : TypedResults.Ok(query.Result);
    }

    public async Task<Ok<long>> CreateAsync(
        [FromServices] ILocalEventBus eventBus,
        CreateModelDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateModelCommand(dto);
        await eventBus.PublishAsync(command, cancellationToken);
        return TypedResults.Ok(command.Id);
    }

    public async Task<Ok> UpdateAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        UpdateModelDto dto,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new UpdateModelCommand(id, dto), cancellationToken);
        return TypedResults.Ok();
    }

    public async Task<Ok> DeleteAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new DeleteModelCommand(id), cancellationToken);
        return TypedResults.Ok();
    }
}
