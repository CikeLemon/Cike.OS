using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.EndpointFilters;
using Cike.Contracts.EntityDtos;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;
using Cike.OS.Application.UserToolConfigurations.Commands;
using Cike.OS.Application.UserToolConfigurations.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cike.OS.Service.Open.Services;

/// <summary>用户工具配置端点：Get / Save / Delete 以工具 Id 为键（当前用户 + 工具唯一），Get 给编辑弹窗回填；PagedList 给工具参数页的卡片，ToolOptions 与 SaveBatch 给批量新增弹窗。</summary>
[AutoValidation]
public class UserToolConfigurationService : MinimalApiServiceBase
{
    public async Task<Results<Ok<UserToolConfigurationDetailDto>, NotFound>> GetAsync(
        [FromServices] ILocalEventBus eventBus,
        long id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserToolConfigurationDetailQuery(id);
        await eventBus.PublishAsync(query, cancellationToken);
        return query.Result == null ? TypedResults.NotFound() : TypedResults.Ok(query.Result);
    }

    public async Task<Ok<PagedResultDto<UserToolConfigurationItemDto>>> GetPagedListAsync(
        [FromServices] ILocalEventBus eventBus,
        string? keyword,
        [AsParameters] PagedAndSortedResultRequest paging,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserToolConfigurationPagedListQuery(keyword, paging);
        await eventBus.PublishAsync(query, cancellationToken);
        return TypedResults.Ok(query.Result ?? new PagedResultDto<UserToolConfigurationItemDto>(0, []));
    }

    public async Task<Ok<List<UserToolConfigurationToolOptionItemDto>>> GetToolOptionsAsync(
        [FromServices] ILocalEventBus eventBus,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserToolConfigurationToolOptionsQuery();
        await eventBus.PublishAsync(query, cancellationToken);
        return TypedResults.Ok(query.Result ?? []);
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

    public async Task<Ok> SaveBatchAsync(
        [FromServices] ILocalEventBus eventBus,
        List<SaveUserToolConfigurationDto> dto,
        CancellationToken cancellationToken = default)
    {
        await eventBus.PublishAsync(new SaveUserToolConfigurationsCommand(dto), cancellationToken);
        return TypedResults.Ok();
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
