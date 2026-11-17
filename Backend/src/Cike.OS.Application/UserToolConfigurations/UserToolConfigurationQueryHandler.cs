using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;
using Cike.OS.Application.UserToolConfigurations.Queries;
using Cike.OS.Domain.Entities;
using Cike.Security;
using Mapster;

namespace Cike.OS.Application.UserToolConfigurations;

public class UserToolConfigurationQueryHandler(
    IReadOnlyRepository<UserToolConfiguration, long> userToolConfigurationRepository,
    ICurrentUser currentUser)
{
    [LocalEventHandler]
    public async Task GetAsync(GetUserToolConfigurationQuery query, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.Id
            ?? throw new UserFriendlyException("Current user id is required.");

        var configuration = await userToolConfigurationRepository.GetListAsync(
            x => x.ToolId == query.ToolId && x.UserId == userId,
            cancellationToken: cancellationToken);

        query.Result = configuration.Count == 0
            ? null
            : configuration[0].Adapt<UserToolConfigurationDto>();
    }
}
