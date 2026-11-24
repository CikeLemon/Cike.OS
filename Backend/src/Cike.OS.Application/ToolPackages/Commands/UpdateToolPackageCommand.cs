using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.ToolPackages.Dtos;

namespace Cike.OS.Application.ToolPackages.Commands;

/// <summary>Id 在路由，DTO 只承载可变字段。</summary>
public record UpdateToolPackageCommand(long Id, UpdateToolPackageDto Dto) : Command;
