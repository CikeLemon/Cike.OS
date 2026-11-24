using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.Tools.Dtos;

namespace Cike.OS.Application.Tools.Commands;

/// <summary>Id 在路由，DTO 只承载可变字段。</summary>
public record UpdateToolCommand(long Id, UpdateToolDto Dto) : Command;
