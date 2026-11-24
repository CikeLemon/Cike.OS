using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;

namespace Cike.OS.Application.ToolPackageCategories.Queries;

public record GetToolPackageCategoryQuery(long Id) : Query<ToolPackageCategoryDto?>;
