using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.EndpointFilters;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;
using Cike.OS.Application.UserToolConfigurations.Commands;
using Cike.OS.Application.UserToolConfigurations.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cike.OS.Service.Open.Services;

[AutoValidation]
public class UserToolConfigurationService : MinimalApiServiceBase
{
    public async Task<Results<Ok<UserToolConfigurationDto>, NotFound>> GetAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserToolConfigurationQuery(id);
        await eventBus.PublishAsync(query, cancellationToken);
        return query.Result == null ? TypedResults.NotFound() : TypedResults.Ok(query.Result);
    }

    public async Task<Ok<long>> SaveAsync(
        [FromServices] ILocalEventBus eventBus,
        SaveUserToolConfigurationDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new SaveUserToolConfigurationCommand(dto);
        await eventBus.PublishAsync(command, cancellationToken);
        return TypedResults.Ok(command.ToolId);
    }

    public async Task<Ok> DeleteAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new DeleteUserToolConfigurationCommand(id), cancellationToken);
        return TypedResults.Ok();
    }
}
