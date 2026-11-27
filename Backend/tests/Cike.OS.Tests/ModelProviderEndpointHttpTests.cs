using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cike.OS.Tests.Infrastructure;
using Xunit;

namespace Cike.OS.Tests;

/// <summary>
/// 接入点五条约定路由的 HTTP 形状：用真实宿主管线打一遍——动词、路径、列表查询串的参数绑定、校验与业务拒绝两种 400、详情的 404、租户隔离。
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

    private static object EndpointBody(long modelProviderId, string name = "官方") => new
    {
        modelProviderId,
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

    private static async Task<long> CreateEndpointAsync(HttpClient http, long modelProviderId, string name = "官方")
    {
        return await ReadIdAsync(await http.PostAsync("/api/v1/ModelProviderEndpoints", Json(EndpointBody(modelProviderId, name))));
    }

    private static async Task<JsonElement> GetListAsync(HttpClient http, long modelProviderId)
    {
        var response = await http.GetAsync($"/api/v1/ModelProviderEndpoints/List?modelProviderId={modelProviderId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task GetDetail_OfUnknownEndpoint_Returns404()
    {
        using var http = _host.CreateClient();

        var response = await http.GetAsync("/api/v1/ModelProviderEndpoints/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetList_FiltersByProviderQuery_AndReturnsEmptyForUnknownProvider()
    {
        using var http = _host.CreateClient();
        var firstId = await CreateProviderAsync(http, $"http-a-{Guid.NewGuid():N}");
        var secondId = await CreateProviderAsync(http, $"http-b-{Guid.NewGuid():N}");
        await CreateEndpointAsync(http, firstId);

        // 查询串参数按名绑定：换一个提供方 Id 得到不同结果；不存在的提供方就是空数组
        Assert.Single((await GetListAsync(http, firstId)).EnumerateArray());
        Assert.Empty((await GetListAsync(http, secondId)).EnumerateArray());
        Assert.Empty((await GetListAsync(http, 999999)).EnumerateArray());
    }

    [Fact]
    public async Task Post_CreatesEndpoint_ThenGetDetailReturnsIt()
    {
        using var http = _host.CreateClient();
        var providerId = await CreateProviderAsync(http, $"http-{Guid.NewGuid():N}");

        var response = await http.PostAsync("/api/v1/ModelProviderEndpoints", Json(EndpointBody(providerId)));

        // 新建返回的 Id 是字符串形式（long → string），详情能按它取回
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var endpointId = await ReadIdAsync(response);
        Assert.True(endpointId > 0);

        var detail = await http.GetFromJsonAsync<JsonElement>($"/api/v1/ModelProviderEndpoints/{endpointId}");
        Assert.Equal(endpointId.ToString(), detail.GetProperty("id").GetString());
        Assert.Equal(providerId.ToString(), detail.GetProperty("modelProviderId").GetString());
        Assert.Equal("官方", detail.GetProperty("name").GetString());
        Assert.Equal(1, detail.GetProperty("protocol").GetInt32());
        Assert.Equal(1, detail.GetProperty("setting").GetProperty("type").GetInt32());
    }

    [Fact]
    public async Task PutAndDelete_TargetTheEndpointOnTheRoute()
    {
        using var http = _host.CreateClient();
        var providerId = await CreateProviderAsync(http, $"swap-{Guid.NewGuid():N}");
        var endpointId = await CreateEndpointAsync(http, providerId);

        // 修改：路由上只有接入点 Id，请求体里没有归属与协议
        var updated = await http.PutAsync($"/api/v1/ModelProviderEndpoints/{endpointId}", Json(new
        {
            name = "改过了",
            baseUrl = "https://vip.api.openai.com/v1",
            apiKey = "sk-new",
            setting = new { type = 1 },
            mappings = Array.Empty<object>(),
        }));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("改过了", Assert.Single((await GetListAsync(http, providerId)).EnumerateArray()).GetProperty("name").GetString());

        // 删除后再删一次：接入点已不存在
        var deleted = await http.DeleteAsync($"/api/v1/ModelProviderEndpoints/{endpointId}");
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Empty((await GetListAsync(http, providerId)).EnumerateArray());

        var again = await http.DeleteAsync($"/api/v1/ModelProviderEndpoints/{endpointId}");
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Equal("接入点不存在。", JsonSerializer.Deserialize<string>(await again.Content.ReadAsStringAsync(), Web));
    }

    [Fact]
    public async Task Post_InvalidBody_ReturnsProblemDetails400()
    {
        using var http = _host.CreateClient();
        var providerId = await CreateProviderAsync(http, $"invalid-{Guid.NewGuid():N}");

        var response = await http.PostAsync("/api/v1/ModelProviderEndpoints", Json(EndpointBody(providerId, "")));

        // FluentValidation 失败：400 + application/problem+json，错误按属性名分组
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"name\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_OnUnknownProvider_ReturnsMessage400()
    {
        using var http = _host.CreateClient();

        var response = await http.PostAsync("/api/v1/ModelProviderEndpoints", Json(EndpointBody(999999)));

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
        var endpointId = await CreateEndpointAsync(tenant1, providerId);

        // 租户 0 看不到租户 1 的接入点：列表为空、详情 404、修改按「不存在」被拒
        using var tenant0 = _host.CreateClient();
        Assert.Empty((await GetListAsync(tenant0, providerId)).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await tenant0.GetAsync($"/api/v1/ModelProviderEndpoints/{endpointId}")).StatusCode);

        var update = await tenant0.PutAsync($"/api/v1/ModelProviderEndpoints/{endpointId}", Json(new
        {
            name = "抢占",
            baseUrl = "https://x",
            apiKey = "k",
            setting = new { type = 1 },
            mappings = Array.Empty<object>(),
        }));
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        Assert.Equal("接入点不存在。", JsonSerializer.Deserialize<string>(await update.Content.ReadAsStringAsync(), Web));

        // 带上租户头就能看到
        Assert.Single((await GetListAsync(tenant1, providerId)).EnumerateArray());
    }
}
