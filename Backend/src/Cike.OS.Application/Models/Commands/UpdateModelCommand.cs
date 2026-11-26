using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.Models.Dtos;

namespace Cike.OS.Application.Models.Commands;

/// <summary>Id 在路由，DTO 只承载可变字段。</summary>
public record UpdateModelCommand(long Id, UpdateModelDto Dto) : Command;
