using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.ToolPackages.Commands;
using Cike.OS.Domain.Entities;

namespace Cike.OS.Application.ToolPackages;

public class ToolPackageCommandHandler(
    IRepository<ToolPackage, long> toolPackageRepository,
    IReadOnlyRepository<ToolPackageCategory, long> categoryRepository,
    IReadOnlyRepository<Tool, long> toolRepository)
{
    [LocalEventHandler]
    public async Task CreateAsync(CreateToolPackageCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;

        await EnsureCategoryExistsAsync(dto.ToolPackageCategoryId, cancellationToken);
        await EnsureCodeUniqueAsync(dto.Code, null, cancellationToken);

        var package = ToolPackage.Create(
            dto.Code, dto.Name, dto.Description, dto.Icon, dto.Readme, dto.ToolPackageCategoryId, dto.Status);

        await toolPackageRepository.InsertAsync(package, cancellationToken: cancellationToken);
        command.Id = package.Id;
    }

    [LocalEventHandler]
    public async Task UpdateAsync(UpdateToolPackageCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        var package = await toolPackageRepository.GetAsync(command.Id, cancellationToken);

        await EnsureCategoryExistsAsync(dto.ToolPackageCategoryId, cancellationToken);
        await EnsureCodeUniqueAsync(dto.Code, package.Id, cancellationToken);

        package.Update(dto.Code, dto.Name, dto.Description, dto.Icon, dto.Readme, dto.ToolPackageCategoryId, dto.Status);
        await toolPackageRepository.UpdateAsync(package, cancellationToken: cancellationToken);
    }

    [LocalEventHandler]
    public async Task DeleteAsync(DeleteToolPackageCommand command, CancellationToken cancellationToken = default)
    {
        var package = await toolPackageRepository.GetAsync(command.Id, cancellationToken);

        var toolCount = await toolRepository.GetCountAsync(x => x.ToolPackageId == package.Id, cancellationToken);
        if (toolCount > 0)
        {
            throw new UserFriendlyException("该工具包下仍有工具，请先删除工具后再删除工具包。");
        }

        await toolPackageRepository.DeleteAsync(package, cancellationToken: cancellationToken);
    }

    private async Task EnsureCategoryExistsAsync(long categoryId, CancellationToken cancellationToken)
    {
        _ = await categoryRepository.GetAsync(categoryId, cancellationToken);
    }

    /// <summary>同租户内 Code 唯一（租户与软删过滤由全局过滤器保证）；索引非唯一，查重只在应用侧。</summary>
    private async Task EnsureCodeUniqueAsync(string code, long? excludeId, CancellationToken cancellationToken)
    {
        var normalized = code.Trim();
        var count = await toolPackageRepository.GetCountAsync(x => x.Code == normalized && x.Id != excludeId, cancellationToken);
        if (count > 0)
        {
            throw new UserFriendlyException($"工具包编码「{normalized}」已存在。");
        }
    }
}
