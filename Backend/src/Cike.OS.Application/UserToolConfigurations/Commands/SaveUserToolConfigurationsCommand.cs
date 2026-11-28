using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;

namespace Cike.OS.Application.UserToolConfigurations.Commands;

/// <summary>批量保存：一次提交多个工具的参数取值（从 Agent 跳转过来时往往有多个工具要配）。</summary>
public record SaveUserToolConfigurationsCommand(List<SaveUserToolConfigurationDto> Items) : Command;
