using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.Extensions;
using Cike.AspNetCore.MinimalAPIs.Options;
using Cike.AspNetCore.Swagger;
using Cike.Core.Modularity;
using Cike.FluentValidation;
using Cike.OS.Application;
using Cike.OS.Application.SkillPackages;
using Cike.OS.EntityFrameworkCore;
using Cike.OS.Service.Open.Swagger;
using Cike.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.OpenApi;
using System.Reflection;
using System.Text.Json.Serialization;

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

        // 开发态没有认证也就没有当前用户：用户工具配置按当前用户隔离，给一个固定的开发用户
        if (environment.IsDevelopment())
        {
            context.Services.Replace(ServiceDescriptor.Singleton<ICurrentUser>(new DevelopmentCurrentUser()));
        }

        // 请求 / 响应 JSON 与持久化共用同一容忍度：多态判别键 type 不要求在对象首位
        context.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.AllowOutOfOrderMetadataProperties = true;
        });

        context.Services.AddCikeSwagger("Cike.OS", options =>
        {
            options.MapType<long>(() => new OpenApiSchema { Type = JsonSchemaType.String, Format = "int64" });
            options.SchemaFilter<JsonPolymorphicDiscriminatorSchemaFilter>();
            options.SelectDiscriminatorNameUsing(type =>
            {
                var baseType = JsonPolymorphicHelper.GetBaseType(type, out var attr);
                if (baseType != null)
                {
                    return attr!.TypeDiscriminatorPropertyName ?? "$type";
                }

                return null;
            });

            // mapping 条目只在判别值 selector 返回非 null 时生成（Swashbuckle 默认 selector 返回 null），反射读 [JsonDerivedType] 的判别值
            options.SelectDiscriminatorValueUsing(type =>
            {
                var baseType = JsonPolymorphicHelper.GetBaseType(type, out _);
                return baseType?.GetCustomAttributes<JsonDerivedTypeAttribute>(false)
                    .FirstOrDefault(attr => attr.DerivedType == type)
                    ?.TypeDiscriminator?.ToString();
            });
        });

        // 请求体与 multipart 上限跟随技能包大小上限，再留 1MB 给 multipart 信封（boundary / 头部），
        // 否则贴边的合法 zip 会先被 Kestrel 以 413 拦下，到不了用例里的友好提示（Kestrel 默认 ~28.6MB 也低于配置的 50MB）
        var maxUploadBytes = context.Services.GetConfiguration().GetValue<long>($"{SkillStorageOptions.SectionName}:{nameof(SkillStorageOptions.MaxUploadBytes)}", 50L * 1024 * 1024)
            + 1024 * 1024;
        context.Services.Configure<KestrelServerOptions>(options => options.Limits.MaxRequestBodySize = maxUploadBytes);
        context.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maxUploadBytes);
    }

    public override async Task InitializeAsync(ApplicationInitializationContext context)
    {
        await base.InitializeAsync(context);

        context.GetApplicationBuilder().UseMultiTenant();

#if DEBUG
        context.GetApplicationBuilder().UseCikeSwaggerUI("Cike.OS");
#endif
    }
}
