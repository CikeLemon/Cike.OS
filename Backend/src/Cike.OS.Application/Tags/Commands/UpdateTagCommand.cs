using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.Tags.Dtos;

namespace Cike.OS.Application.Tags.Commands;

/// <summary>Id 在路由，DTO 只承载可变字段（键不可变）。</summary>
public record UpdateTagCommand(long Id, UpdateTagDto Dto) : Command;
