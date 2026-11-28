using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;

namespace Cike.OS.Application.UserToolConfigurations.Queries;

public record GetUserToolConfigurationDetailQuery(long ToolId) : Query<UserToolConfigurationDetailDto?>;
