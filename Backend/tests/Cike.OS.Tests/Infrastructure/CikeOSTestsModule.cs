using Cike.Core.Extensions;
using Cike.Core.Modularity;
using Cike.EventBus.Local;
using Cike.OS.Application;
using Cike.OS.EntityFrameworkCore;
using Cike.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cike.OS.Tests.Infrastructure;

[DependsOn(
    typeof(CikeOSApplicationModule),
    typeof(CikeOSEntityFrameworkCoreModule),
    typeof(CikeEventBusLocalModule))]
public class CikeOSTestsModule : CikeModule
{
    public override Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<FakeCurrentUser>();
        context.Services.Replace(ServiceDescriptor.Singleton<ICurrentUser>(sp => sp.GetRequiredService<FakeCurrentUser>()));
        return base.ConfigureServicesAsync(context);
    }
}
