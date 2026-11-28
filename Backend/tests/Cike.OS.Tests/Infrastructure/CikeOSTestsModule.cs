using Cike.Core.Extensions;
using Cike.Core.Modularity;
using Cike.EventBus.Local;
using Cike.FluentValidation;
using Cike.OS.Application;
using Cike.OS.EntityFrameworkCore;
using Cike.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cike.OS.Tests.Infrastructure;

/// <summary>
/// 测试宿主的模块。也依赖 CikeFluentValidationModule：HTTP 测试宿主与本宿主共用进程级的「已注册 Validator 类型」缓存
/// （AutoValidationEndpointFilterProvider 首次构造时从 ModuleLoader.Services 快照扫描），本宿主的快照里也要有 Validator，
/// 否则哪个宿主最后初始化会决定 HTTP 测试里 [AutoValidation] 是否生效。
/// Validator 只在 HTTP 管线里被执行，本宿主直接 Publish Command 的用例测试仍不触发校验。
/// </summary>
[DependsOn(
    typeof(CikeOSApplicationModule),
    typeof(CikeOSEntityFrameworkCoreModule),
    typeof(CikeEventBusLocalModule),
    typeof(CikeFluentValidationModule))]
public class CikeOSTestsModule : CikeModule
{
    public override Task ConfigureServicesAsync(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<FakeCurrentUser>();
        context.Services.Replace(ServiceDescriptor.Singleton<ICurrentUser>(sp => sp.GetRequiredService<FakeCurrentUser>()));
        return base.ConfigureServicesAsync(context);
    }
}
