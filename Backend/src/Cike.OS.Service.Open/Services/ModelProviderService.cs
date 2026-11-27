using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.EndpointFilters;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Commands;
using Cike.OS.Application.ModelProviders.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cike.OS.Service.Open.Services;

/// <summary>
/// 模型提供方的端点。接入点是提供方下的子资源，读写都走它自己的路由，端点方法放在这里；
/// 嵌套路径约定路由表达不了，接入点的四个方法各自用显式路由（两个 id 段按参数名绑定）。
/// </summary>
[AutoValidation]
public class ModelProviderService : MinimalApiServiceBase
{
    public async Task<Ok<List<ModelProviderListDto>>> GetListAsync(
        [FromServices] ILocalEventBus eventBus,
        CancellationToken cancellationToken = default)
    {
        var query = new GetModelProviderListQuery();
        await eventBus.PublishAsync(query, cancellationToken);
        return TypedResults.Ok(query.Result ?? []);
    }

    public async Task<Results<Ok<ModelProviderDto>, NotFound>> GetAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetModelProviderQuery(id);
        await eventBus.PublishAsync(query, cancellationToken);
        return query.Result == null ? TypedResults.NotFound() : TypedResults.Ok(query.Result);
    }

    public async Task<Ok<long>> CreateAsync(
        [FromServices] ILocalEventBus eventBus,
        CreateModelProviderDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateModelProviderCommand(dto);
        await eventBus.PublishAsync(command, cancellationToken);
        return TypedResults.Ok(command.Id);
    }

    public async Task<Ok> UpdateAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        UpdateModelProviderDto dto,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new UpdateModelProviderCommand(id, dto), cancellationToken);
        return TypedResults.Ok();
    }

    public async Task<Ok> DeleteAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new DeleteModelProviderCommand(id), cancellationToken);
        return TypedResults.Ok();
    }

    [MinimalApiRoute("api/v1/ModelProviders/{id}/Endpoints", "GET")]
    public async Task<Results<Ok<List<ModelProviderEndpointDto>>, NotFound>> GetEndpointListAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetModelProviderEndpointListQuery(id);
        await eventBus.PublishAsync(query, cancellationToken);
        return query.Result == null ? TypedResults.NotFound() : TypedResults.Ok(query.Result);
    }

    [MinimalApiRoute("api/v1/ModelProviders/{id}/Endpoints", "POST")]
    public async Task<Ok<long>> CreateEndpointAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CreateModelProviderEndpointDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateModelProviderEndpointCommand(id, dto);
        await eventBus.PublishAsync(command, cancellationToken);
        return TypedResults.Ok(command.Id);
    }

    [MinimalApiRoute("api/v1/ModelProviders/{id}/Endpoints/{endpointId}", "PUT")]
    public async Task<Ok> UpdateEndpointAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        long endpointId,
        UpdateModelProviderEndpointDto dto,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new UpdateModelProviderEndpointCommand(id, endpointId, dto), cancellationToken);
        return TypedResults.Ok();
    }

    [MinimalApiRoute("api/v1/ModelProviders/{id}/Endpoints/{endpointId}", "DELETE")]
    public async Task<Ok> DeleteEndpointAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        long endpointId,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new DeleteModelProviderEndpointCommand(id, endpointId), cancellationToken);
        return TypedResults.Ok();
    }
}
