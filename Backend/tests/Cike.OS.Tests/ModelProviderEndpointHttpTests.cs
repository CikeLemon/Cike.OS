using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cike.OS.Tests.Infrastructure;
using Xunit;

namespace Cike.OS.Tests;

/// <summary>
/// 接入点四条路由的 HTTP 形状：约定路由表达不了嵌套路径，这四个端点是显式指定路由的，
/// 所以在这里用真实宿主管线打一遍——动词、路径、两个 id 段的参数绑定、校验与业务拒绝两种 400、列表 404、租户隔离。
/// 业务规则（重名、模型不存在、映射同步）由 ModelProviderTests 经命令 / 查询接缝覆盖，这里不重复。
/// </summary>
public class ModelProviderEndpointHttpTests : IClassFixture<ServiceOpenTestHost>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly ServiceOpenTestHost _host;

    public ModelProviderEndpointHttpTests(ServiceOpenTestHost host)
    {
        _host = host;
    }

    private static StringContent Json(object body) => new(JsonSerializer.Serialize(body, Web), Encoding.UTF8, "application/json");

    private static object EndpointBody(string name = "官方") => new
    {
        name,
        protocol = 1,
        baseUrl = "https://api.openai.com/v1",
        apiKey = "sk-test",
        setting = new { type = 1 },
        mappings = Array.Empty<object>(),
    };

    /// <summary>响应体是 JSON 字符串字面量（框架把 long 序列化成字符串），按字符串读出再转 long。</summary>
    private static async Task<long> ReadIdAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return long.Parse(JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync(), Web)!);
    }

    /// <summary>新建一个提供方，返回它的 Id。</summary>
    private static async Task<long> CreateProviderAsync(HttpClient http, string name)
    {
        return await ReadIdAsync(await http.PostAsync("/api/v1/ModelProviders", Json(new { name })));
    }

    private static async Task<long> CreateEndpointAsync(HttpClient http, long providerId, string name = "官方")
    {
        return await ReadIdAsync(await http.PostAsync($"/api/v1/ModelProviders/{providerId}/Endpoints", Json(EndpointBody(name))));
    }

    private static async Task<JsonElement> GetEndpointListAsync(HttpClient http, long providerId)
    {
        var response = await http.GetAsync($"/api/v1/ModelProviders/{providerId}/Endpoints");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Get_EndpointList_OfUnknownProvider_Returns404()
    {
        using var http = _host.CreateClient();

        var response = await http.GetAsync("/api/v1/ModelProviders/999999/Endpoints");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreatesEndpoint_ThenGetListReturnsIt()
    {
        using var http = _host.CreateClient();
        var providerId = await CreateProviderAsync(http, $"http-{Guid.NewGuid():N}");

        var response = await http.PostAsync($"/api/v1/ModelProviders/{providerId}/Endpoints", Json(EndpointBody()));

        // 新建返回的 Id 是字符串形式（long → string）
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var endpointId = await ReadIdAsync(response);
        Assert.True(endpointId > 0);

        var endpoint = Assert.Single((await GetEndpointListAsync(http, providerId)).EnumerateArray());
        Assert.Equal(endpointId.ToString(), endpoint.GetProperty("id").GetString());
        Assert.Equal("官方", endpoint.GetProperty("name").GetString());
        Assert.Equal(1, endpoint.GetProperty("protocol").GetInt32());
        Assert.Equal(1, endpoint.GetProperty("setting").GetProperty("type").GetInt32());
    }

    [Fact]
    public async Task PutAndDelete_RouteIdsBindToTheirOwnSegments()
    {
        using var http = _host.CreateClient();
        var providerId = await CreateProviderAsync(http, $"swap-{Guid.NewGuid():N}");
        var endpointId = await CreateEndpointAsync(http, providerId);

        // 两个 id 段各绑各的：把提供方 Id 与接入点 Id 对调，提供方查不到
        var swapped = await http.PutAsync($"/api/v1/ModelProviders/{endpointId}/Endpoints/{providerId}", Json(new
        {
            name = "对调",
            baseUrl = "https://x",
            apiKey = "k",
            setting = new { type = 1 },
            mappings = Array.Empty<object>(),
        }));
        Assert.Equal(HttpStatusCode.BadRequest, swapped.StatusCode);
        Assert.Contains("模型提供方不存在", await swapped.Content.ReadAsStringAsync());

        // 修改：路由上是提供方 Id + 接入点 Id，请求体里没有协议
        var updated = await http.PutAsync($"/api/v1/ModelProviders/{providerId}/Endpoints/{endpointId}", Json(new
        {
            name = "改过了",
            baseUrl = "https://vip.api.openai.com/v1",
            apiKey = "sk-new",
            setting = new { type = 1 },
            mappings = Array.Empty<object>(),
        }));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("改过了", Assert.Single((await GetEndpointListAsync(http, providerId)).EnumerateArray()).GetProperty("name").GetString());

        // 删除后再删一次：接入点已不在名下
        var deleted = await http.DeleteAsync($"/api/v1/ModelProviders/{providerId}/Endpoints/{endpointId}");
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Empty((await GetEndpointListAsync(http, providerId)).EnumerateArray());

        var again = await http.DeleteAsync($"/api/v1/ModelProviders/{providerId}/Endpoints/{endpointId}");
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Contains("接入点不存在", await again.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_InvalidBody_ReturnsProblemDetails400()
    {
        using var http = _host.CreateClient();
        var providerId = await CreateProviderAsync(http, $"invalid-{Guid.NewGuid():N}");

        var response = await http.PostAsync($"/api/v1/ModelProviders/{providerId}/Endpoints", Json(EndpointBody("")));

        // FluentValidation 失败：400 + application/problem+json，错误按属性名分组
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"name\"", body);
    }

    [Fact]
    public async Task Post_OnUnknownProvider_ReturnsPlainText400()
    {
        using var http = _host.CreateClient();

        var response = await http.PostAsync("/api/v1/ModelProviders/999999/Endpoints", Json(EndpointBody()));

        // 业务拒绝：400 + 消息文本以 JSON 字串给出（与 FluentValidation 的 ProblemDetails 是两种形状）
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("模型提供方不存在。", JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync(), Web));
    }

    [Fact]
    public async Task Endpoints_AreIsolatedByTenantHeader()
    {
        using var tenant1 = _host.CreateClient(tenantId: 1);
        var providerId = await CreateProviderAsync(tenant1, $"tenant1-{Guid.NewGuid():N}");
        await CreateEndpointAsync(tenant1, providerId);

        // 租户 0 看不到租户 1 的提供方
        using var tenant0 = _host.CreateClient();
        var invisible = await tenant0.GetAsync($"/api/v1/ModelProviders/{providerId}/Endpoints");
        Assert.Equal(HttpStatusCode.NotFound, invisible.StatusCode);

        // 带上租户头就能看到
        Assert.Single((await GetEndpointListAsync(tenant1, providerId)).EnumerateArray());
    }
}
