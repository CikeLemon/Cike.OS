using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;

namespace Cike.OS.Application.ToolPackageCategories.Commands;

/// <summary>Id 在路由，DTO 只承载可变字段。</summary>
public record UpdateToolPackageCategoryCommand(long Id, UpdateToolPackageCategoryDto Dto) : Command;
