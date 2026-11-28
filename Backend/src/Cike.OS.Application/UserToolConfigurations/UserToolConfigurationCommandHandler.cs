using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.UserToolConfigurations.Commands;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Services;
using Cike.Security;

namespace Cike.OS.Application.UserToolConfigurations;

public class UserToolConfigurationCommandHandler(
    IRepository<UserToolConfiguration, long> userToolConfigurationRepository,
    IReadOnlyRepository<Tool, long> toolRepository,
    ICurrentUser currentUser,
    ToolParameterValueResolver toolParameterValueResolver)
{
    private string RequireUserId() => currentUser.Id
        ?? throw new UserFriendlyException("Current user id is required.");

    [LocalEventHandler]
    public async Task SaveAsync(SaveUserToolConfigurationCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        var tool = await toolRepository.GetAsync(dto.ToolId, cancellationToken);
        var values = toolParameterValueResolver.ResolveUserValues(tool.Parameters, dto.Values);

        var userId = RequireUserId();

        var existing = await userToolConfigurationRepository.GetListAsync(
            x => x.ToolId == tool.Id && x.UserId == userId,
            cancellationToken: cancellationToken);

        if (existing.Count == 0)
        {
            var configuration = new UserToolConfiguration
            {
                UserId = userId,
                ToolId = tool.Id,
                Values = values.ToDictionary(x => x.Key, x => x.Value),
            };

            await userToolConfigurationRepository.InsertAsync(configuration, cancellationToken: cancellationToken);
            command.ToolId = configuration.ToolId;
        }
        else
        {
            var configuration = existing[0];
            configuration.Values = values.ToDictionary(x => x.Key, x => x.Value);
            await userToolConfigurationRepository.UpdateAsync(configuration, cancellationToken: cancellationToken);
            command.ToolId = configuration.ToolId;
        }
    }

    /// <summary>
    /// 批量保存：工具与当前用户的已有配置各一次批量取回，循环里只做内存赋值；
    /// 必填参数缺失的解析异常在任何写入之前抛出，整批不保存。
    /// </summary>
    [LocalEventHandler]
    public async Task SaveBatchAsync(SaveUserToolConfigurationsCommand command, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        var toolIds = command.Items.Select(x => x.ToolId).Distinct().ToList();
        var tools = await toolRepository.GetListAsync(x => toolIds.Contains(x.Id), "Id asc", cancellationToken);
        if (tools.Count != toolIds.Count)
        {
            throw new UserFriendlyException("部分工具不存在，请刷新后重新选择。");
        }

        var toolsById = tools.ToDictionary(x => x.Id);
        var existing = await userToolConfigurationRepository.GetListAsync(
            x => toolIds.Contains(x.ToolId) && x.UserId == userId,
            "Id asc",
            cancellationToken);
        var existingByToolId = existing.ToDictionary(x => x.ToolId);

        var toInsert = new List<UserToolConfiguration>();
        var toUpdate = new List<UserToolConfiguration>();
        foreach (var item in command.Items)
        {
            var tool = toolsById[item.ToolId];
            var values = toolParameterValueResolver.ResolveUserValues(tool.Parameters, item.Values).ToDictionary(x => x.Key, x => x.Value);

            if (existingByToolId.TryGetValue(item.ToolId, out var configuration))
            {
                configuration.Values = values;
                toUpdate.Add(configuration);
            }
            else
            {
                toInsert.Add(new UserToolConfiguration
                {
                    UserId = userId,
                    ToolId = item.ToolId,
                    Values = values,
                });
            }
        }

        if (toInsert.Count > 0)
        {
            await userToolConfigurationRepository.InsertManyAsync(toInsert, cancellationToken: cancellationToken);
        }

        if (toUpdate.Count > 0)
        {
            await userToolConfigurationRepository.UpdateManyAsync(toUpdate, cancellationToken: cancellationToken);
        }
    }

    [LocalEventHandler]
    public async Task DeleteAsync(DeleteUserToolConfigurationCommand command, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        var existing = await userToolConfigurationRepository.GetListAsync(
            x => x.ToolId == command.ToolId && x.UserId == userId,
            cancellationToken: cancellationToken);

        if (existing.Count > 0)
        {
            await userToolConfigurationRepository.DeleteAsync(existing[0], cancellationToken: cancellationToken);
        }
    }
}
