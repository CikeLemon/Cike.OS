using Cike.AspNetCore.MinimalAPIs;
using Cike.AspNetCore.MinimalAPIs.Extensions;
using Cike.AspNetCore.MinimalAPIs.Options;
using Cike.AspNetCore.Swagger;
using Cike.Core.Modularity;
using Cike.FluentValidation;
using Cike.OS.Application;
using Cike.OS.Application.SkillPackages;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.EntityFrameworkCore;
using Cike.OS.Service.Open.Swagger;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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

        // 请求 / 响应 JSON 与持久化共用同一容忍度：多态判别键 type 不要求在对象首位
        context.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.AllowOutOfOrderMetadataProperties = true;
        });

        context.Services.AddCikeSwagger("Cike.Auth", options =>
        {
            options.MapType<long>(() => new OpenApiSchema { Type = "string", Format = "int64" });
            options.OperationFilter<SwaggerFormFileOperationFilter>();
            options.SchemaFilter<ToolSettingDiscriminatorSchemaFilter>();

            // Swashbuckle 不识别 System.Text.Json 的 [JsonPolymorphic]，显式声明工具设置的多态子类型与判别键（判别值 = ToolType 数值）
            options.UseOneOfForPolymorphism();
            options.SelectSubTypesUsing(type => type == typeof(ToolSetting)
                ? [typeof(SkillToolSetting), typeof(HttpMcpToolSetting), typeof(StdioMcpToolSetting)]
                : []);
            options.SelectDiscriminatorNameUsing(_ => "type");
            options.SelectDiscriminatorValueUsing(type => type switch
            {
                var t when t == typeof(SkillToolSetting) => ((int)ToolType.Skill).ToString(),
                var t when t == typeof(HttpMcpToolSetting) => ((int)ToolType.HttpMcp).ToString(),
                var t when t == typeof(StdioMcpToolSetting) => ((int)ToolType.StdioMcp).ToString(),
                _ => null,
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
        context.GetApplicationBuilder().UseCikeSwaggerUI("Cike.Auth");
#endif
    }
}
