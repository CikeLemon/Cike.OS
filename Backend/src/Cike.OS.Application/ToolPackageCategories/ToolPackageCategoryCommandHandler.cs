using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.ToolPackageCategories.Commands;
using Cike.OS.Domain.Entities;

namespace Cike.OS.Application.ToolPackageCategories;

public class ToolPackageCategoryCommandHandler(
    IRepository<ToolPackageCategory, long> categoryRepository,
    IReadOnlyRepository<ToolPackage, long> toolPackageRepository)
{
    [LocalEventHandler]
    public async Task CreateAsync(CreateToolPackageCategoryCommand command, CancellationToken cancellationToken = default)
    {
        await EnsureNameUniqueAsync(command.Dto.Name, null, cancellationToken);

        var category = ToolPackageCategory.Create(command.Dto.Name);
        await categoryRepository.InsertAsync(category, cancellationToken: cancellationToken);
        command.Id = category.Id;
    }

    [LocalEventHandler]
    public async Task UpdateAsync(UpdateToolPackageCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetAsync(command.Id, cancellationToken);
        await EnsureNameUniqueAsync(command.Dto.Name, category.Id, cancellationToken);

        category.Rename(command.Dto.Name);
        await categoryRepository.UpdateAsync(category, cancellationToken: cancellationToken);
    }

    [LocalEventHandler]
    public async Task DeleteAsync(DeleteToolPackageCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetAsync(command.Id, cancellationToken);

        var packageCount = await toolPackageRepository.GetCountAsync(x => x.ToolPackageCategoryId == category.Id, cancellationToken);
        if (packageCount > 0)
        {
            throw new UserFriendlyException("该分类下仍有工具包，请先移除或转移后再删除。");
        }

        await categoryRepository.DeleteAsync(category, cancellationToken: cancellationToken);
    }

    /// <summary>分类名称同租户内唯一（租户与软删过滤由全局过滤器保证；软删后同名可重建）。</summary>
    private async Task EnsureNameUniqueAsync(string name, long? excludeId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim();
        var count = await categoryRepository.GetCountAsync(x => x.Name == normalized && x.Id != excludeId, cancellationToken);
        if (count > 0)
        {
            throw new UserFriendlyException($"工具包分类「{normalized}」已存在。");
        }
    }
}
