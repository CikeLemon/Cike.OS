using Cike.Caching;
using Cike.Cqrs;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts;
using Cike.OS.Domain;

namespace Cike.OS.Application;

[DependsOn(
    typeof(CikeCqrsModule),
    typeof(CikeEventBusLocalModule),
    typeof(CikeCachingModule),

    typeof(CikeOSApplicationContractsModule),
    typeof(CikeOSDomainModule)
)]
public class CikeOSApplicationModule : CikeModule
{
}
