using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.EndpointFilters;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;
using Cike.OS.Application.ToolPackageCategories.Commands;
using Cike.OS.Application.ToolPackageCategories.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cike.OS.Service.Open.Services;

[AutoValidation]
public class ToolPackageCategoryService : MinimalApiServiceBase
{
    public async Task<Ok<List<ToolPackageCategoryDto>>> GetListAsync(
        [FromServices] ILocalEventBus eventBus,
        CancellationToken cancellationToken = default)
    {
        var query = new GetToolPackageCategoryListQuery();
        await eventBus.PublishAsync(query, cancellationToken);
        return TypedResults.Ok(query.Result ?? []);
    }

    public async Task<Results<Ok<ToolPackageCategoryDto>, NotFound>> GetAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetToolPackageCategoryQuery(id);
        await eventBus.PublishAsync(query, cancellationToken);
        return query.Result == null ? TypedResults.NotFound() : TypedResults.Ok(query.Result);
    }

    public async Task<Ok<long>> CreateAsync(
        [FromServices] ILocalEventBus eventBus,
        CreateToolPackageCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateToolPackageCategoryCommand(dto);
        await eventBus.PublishAsync(command, cancellationToken);
        return TypedResults.Ok(command.Id);
    }

    public async Task<Ok> UpdateAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        UpdateToolPackageCategoryDto dto,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new UpdateToolPackageCategoryCommand(id, dto), cancellationToken);
        return TypedResults.Ok();
    }

    public async Task<Ok> DeleteAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new DeleteToolPackageCategoryCommand(id), cancellationToken);
        return TypedResults.Ok();
    }
}
