using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.Agents.Dtos;

namespace Cike.OS.Application.Agents.Commands;

/// <summary>Id 在路由，DTO 承载全部字段与期望的完整工具条目列表。</summary>
public record UpdateAgentCommand(long Id, UpdateAgentDto Dto) : Command;
