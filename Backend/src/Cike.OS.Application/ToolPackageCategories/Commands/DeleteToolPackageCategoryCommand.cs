using Cike.Cqrs.Commands;

namespace Cike.OS.Application.ToolPackageCategories.Commands;

public record DeleteToolPackageCategoryCommand(long Id) : Command;
