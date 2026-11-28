using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.EndpointFilters;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ToolPackages.Dtos;
using Cike.OS.Application.ToolPackages.Commands;
using Cike.OS.Application.ToolPackages.Queries;
using Cike.Contracts.EntityDtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cike.OS.Service.Open.Services;

[AutoValidation]
public class ToolPackageService : MinimalApiServiceBase
{
    public async Task<Ok<PagedResultDto<ToolPackageItemDto>>> GetPagedListAsync(
        [FromServices] ILocalEventBus eventBus,
        [AsParameters] GetToolPackagePagedListRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var query = new GetToolPackagePagedListQuery(dto);
        await eventBus.PublishAsync(query, cancellationToken);
        return TypedResults.Ok(query.Result ?? new PagedResultDto<ToolPackageItemDto>(0, []));
    }

    public async Task<Results<Ok<ToolPackageDetailDto>, NotFound>> GetAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetToolPackageDetailQuery(id);
        await eventBus.PublishAsync(query, cancellationToken);
        return query.Result == null ? TypedResults.NotFound() : TypedResults.Ok(query.Result);
    }

    public async Task<Ok<long>> CreateAsync(
        [FromServices] ILocalEventBus eventBus,
        CreateToolPackageDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateToolPackageCommand(dto);
        await eventBus.PublishAsync(command, cancellationToken);
        return TypedResults.Ok(command.Id);
    }

    public async Task<Ok> UpdateAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        UpdateToolPackageDto dto,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new UpdateToolPackageCommand(id, dto), cancellationToken);
        return TypedResults.Ok();
    }

    public async Task<Ok> DeleteAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new DeleteToolPackageCommand(id), cancellationToken);
        return TypedResults.Ok();
    }
}
