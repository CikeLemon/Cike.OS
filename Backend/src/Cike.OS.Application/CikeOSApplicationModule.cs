using Cike.Caching;
using Cike.Core.Modularity;
using Cike.Cqrs;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts;
using Cike.OS.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cike.OS.Application
{
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
}
