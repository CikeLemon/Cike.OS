using Cike.Workflow.Caching;
using Cike.Workflow.Caching.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cike.OS.Caching;

[DependsOn([
    typeof(CikeCachingModule)
    ])]
public class CikeOSCachingModule : CikeModule
{
    public override async Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        context.Services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(ICacheService<>), typeof(BaseCacheService<>)));

        await base.ConfigureServicesAsync(context);
    }
}
