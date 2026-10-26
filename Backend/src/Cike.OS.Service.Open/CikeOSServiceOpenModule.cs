using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.Extensions;
using Cike.AspNetCore.MinimalAPIs.Options;
using Cike.AspNetCore.Swagger;
using Cike.Core.Modularity;
using Cike.FluentValidation;
using Cike.OS.Application;
using Cike.OS.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace Cike.OS.Service.Open;

[DependsOn(
    typeof(CikeAspNetCoreMinimalApiModule),
    typeof(CikeFluentValidationModule),

    typeof(CikeOSApplicationModule),
    typeof(CikeOSEntityFrameworkCoreModule)
)]
public class CikeOSServiceOpenModule : CikeModule
{
    public override async Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        await base.ConfigureServicesAsync(context);
        var environment = context.Services.GetSingletonInstance<IHostEnvironment>();

        context.Services.Configure<GlobalMinimalApiRouteOptions>(options =>
        {
            options.EnabledAuthorization = !environment.IsDevelopment();
        });

        context.Services.AddCikeSwagger("Cike.Auth", options =>
        {
            options.MapType<long>(() => new OpenApiSchema { Type = "string", Format = "int64" });
        });

    }

    public override async Task InitializeAsync(ApplicationInitializationContext context)
    {
        await base.InitializeAsync(context);

        context.GetApplicationBuilder().UseMultiTenant();

#if DEBUG
        context.GetApplicationBuilder().UseCikeSwaggerUI("Cike.Auth");
#endif
    }
}
