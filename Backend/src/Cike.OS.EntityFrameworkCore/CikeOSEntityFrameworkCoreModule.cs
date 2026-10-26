using Cike.Core.Modularity;
using Cike.Data.EFCore.MySql;
using Cike.Data.Extensions;
using Cike.OS.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cike.OS.EntityFrameworkCore
{
    [DependsOn([
        typeof(CikeDataEFCoreMySqlModule),
        typeof(CikeOSDomainModule),
    ])]
    public class CikeOSEntityFrameworkCoreModule : CikeModule
    {
        public override async Task ConfigureServicesAsync(ServiceConfigurationContext context)
        {
            context.Services.AddCikeDbContext<CikeOSDbContext>();
            await base.ConfigureServicesAsync(context);
        }
    }
}
