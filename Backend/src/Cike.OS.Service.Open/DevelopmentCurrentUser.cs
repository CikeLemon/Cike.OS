using Cike.Security;

namespace Cike.OS.Service.Open;

/// <summary>
/// 开发环境的当前用户：主服务在 Development 下不要求认证、也没有配置 JWT，<see cref="ICurrentUser"/> 读不到任何 Claims；
/// 用户工具配置按「当前用户」隔离，没有用户 Id 就无法使用。这里给一个固定的开发用户，只在 Development 下替换注册。
/// </summary>
public class DevelopmentCurrentUser : ICurrentUser
{
    public const string UserId = "dev-user";

    public string? Id => UserId;

    public string? UserName => UserId;

    public string? Name => "开发用户";

    public string? NickName => null;

    public string? PhoneNumber => null;

    public string? Email => null;

    public long? TenantId => null;

    public string[] Roles { get; } = [];

    public bool IsAuthorization => false;
}
