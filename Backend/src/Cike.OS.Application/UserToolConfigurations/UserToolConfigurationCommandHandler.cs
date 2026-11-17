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
    [LocalEventHandler]
    public async Task SaveAsync(SaveUserToolConfigurationCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        var tool = await toolRepository.GetAsync(dto.ToolId, cancellationToken);
        var values = toolParameterValueResolver.ResolveUserValues(tool.Parameters, dto.Values);

        var userId = currentUser.Id
            ?? throw new UserFriendlyException("Current user id is required.");

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

    [LocalEventHandler]
    public async Task DeleteAsync(DeleteUserToolConfigurationCommand command, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.Id
            ?? throw new UserFriendlyException("Current user id is required.");

        var existing = await userToolConfigurationRepository.GetListAsync(
            x => x.ToolId == command.ToolId && x.UserId == userId,
            cancellationToken: cancellationToken);

        if (existing.Count > 0)
        {
            await userToolConfigurationRepository.DeleteAsync(existing[0], cancellationToken: cancellationToken);
        }
    }
}
