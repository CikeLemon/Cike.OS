using Cike.EventBus.Local;
using Cike.MultiTenancy;
using Cike.OS.Application.SkillPackages.Commands;

namespace Cike.OS.Application.SkillPackages;

public class SkillPackageCommandHandler(ISkillPackageStorage storage, ICurrentTenant currentTenant)
{
    [LocalEventHandler]
    public async Task UploadAsync(UploadSkillPackageCommand command, CancellationToken cancellationToken = default)
    {
        command.Directory = await storage.SaveAsync(command.Content, currentTenant.Id, cancellationToken);
    }
}
