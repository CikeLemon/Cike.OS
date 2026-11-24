using Cike.Caching;
using Cike.Cqrs;
using Cike.EventBus.Local;
using Cike.MultiTenancy;
using Cike.OS.Application.Contracts;
using Cike.OS.Application.SkillPackages;
using Cike.OS.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Cike.OS.Application;

[DependsOn(
    typeof(CikeCqrsModule),
    typeof(CikeEventBusLocalModule),
    typeof(CikeCachingModule),
    typeof(CikeMultiTenancyModule),

    typeof(CikeOSApplicationContractsModule),
    typeof(CikeOSDomainModule)
)]
public class CikeOSApplicationModule : CikeModule
{
    public override Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        context.Services.Configure<SkillStorageOptions>(context.Services.GetConfiguration().GetSection(SkillStorageOptions.SectionName));
        return base.ConfigureServicesAsync(context);
    }
}
