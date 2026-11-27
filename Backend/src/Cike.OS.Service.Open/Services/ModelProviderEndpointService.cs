using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.EndpointFilters;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;
using Cike.OS.Application.ModelProviderEndpoints.Commands;
using Cike.OS.Application.ModelProviderEndpoints.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cike.OS.Service.Open.Services;

/// <summary>
/// 接入点：独立的聚合，顶层资源，全部走约定路由（列表按提供方过滤 / 详情 / 新建 / 更新 / 删除）。
/// 提供商映射随接入点的新建 / 更新提交，没有自己的端点。
/// </summary>
[AutoValidation]
public class ModelProviderEndpointService : MinimalApiServiceBase
{
    public async Task<Ok<List<ModelProviderEndpointDto>>> GetListAsync(
        [FromServices] ILocalEventBus eventBus,
        long modelProviderId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetModelProviderEndpointListQuery(modelProviderId);
        await eventBus.PublishAsync(query, cancellationToken);
        return TypedResults.Ok(query.Result ?? []);
    }

    public async Task<Results<Ok<ModelProviderEndpointDto>, NotFound>> GetAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetModelProviderEndpointQuery(id);
        await eventBus.PublishAsync(query, cancellationToken);
        return query.Result == null ? TypedResults.NotFound() : TypedResults.Ok(query.Result);
    }

    public async Task<Ok<long>> CreateAsync(
        [FromServices] ILocalEventBus eventBus,
        CreateModelProviderEndpointDto dto,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateModelProviderEndpointCommand(dto);
        await eventBus.PublishAsync(command, cancellationToken);
        return TypedResults.Ok(command.Id);
    }

    public async Task<Ok> UpdateAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        UpdateModelProviderEndpointDto dto,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new UpdateModelProviderEndpointCommand(id, dto), cancellationToken);
        return TypedResults.Ok();
    }

    public async Task<Ok> DeleteAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new DeleteModelProviderEndpointCommand(id), cancellationToken);
        return TypedResults.Ok();
    }
}
