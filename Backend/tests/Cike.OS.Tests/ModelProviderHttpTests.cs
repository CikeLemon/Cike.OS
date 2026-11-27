using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cike.OS.Tests.Infrastructure;
using Xunit;

namespace Cike.OS.Tests;

/// <summary>
/// 模型提供方五条约定路由的 HTTP 形状：用真实宿主管线打一遍——动词、路径、多态设置的请求体与响应体、校验与业务拒绝两种 400、详情的 404、租户隔离。
/// 业务规则（重名、模型同步、标签不存在）由 ModelProviderTests 经命令 / 查询接缝覆盖，这里不重复。
/// </summary>
public class ModelProviderHttpTests : IClassFixture<ServiceOpenTestHost>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly ServiceOpenTestHost _host;

    public ModelProviderHttpTests(ServiceOpenTestHost host)
    {
        _host = host;
    }

    private static StringContent Json(object body) => new(JsonSerializer.Serialize(body, Web), Encoding.UTF8, "application/json");

    private static object ProviderBody(string name = "智谱", int protocol = 1, object? setting = null, string baseUrl = "https://api.openai.com/v1", string apiKey = "sk-test") => new
    {
        name,
        protocol,
        baseUrl,
        apiKey,
        setting = setting ?? new { type = protocol },
        models = Array.Empty<object>(),
    };

    private static object ModelBody(string name, string? description = null, long[]? tagIds = null) => new
    {
        name,
        description,
        tagIds = tagIds ?? [],
    };

    /// <summary>响应体是 JSON 字符串字面量（框架把 long 序列化成字符串），按字符串读出再转 long。</summary>
    private static async Task<long> ReadIdAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return long.Parse(JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync(), Web)!);
    }

    private static async Task<long> CreateProviderAsync(HttpClient http, string name) =>
        await ReadIdAsync(await http.PostAsync("/api/v1/ModelProviders", Json(ProviderBody(name))));

    private static async Task<JsonElement> GetDetailAsync(HttpClient http, long providerId)
    {
        var response = await http.GetAsync($"/api/v1/ModelProviders/{providerId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string> ReadMessageAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync(), Web)!;

    [Fact]
    public async Task GetDetail_OfUnknownProvider_Returns404()
    {
        using var http = _host.CreateClient();

        var response = await http.GetAsync("/api/v1/ModelProviders/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreatesProvider_ThenGetDetailReturnsConnectionAndModels()
    {
        using var http = _host.CreateClient();

        var response = await http.PostAsync("/api/v1/ModelProviders", Json(new
        {
            name = "智谱",
            protocol = 2,
            baseUrl = "https://api.anthropic.com",
            apiKey = "ak-test",
            // 多态设置的判别键是 camelCase 的 type，可以不在对象首位
            setting = new { version = "2023-06-01", type = 2 },
            models = new[] { ModelBody("qwen"), ModelBody("glm-5.3", "旗舰") },
        }));

        // 新建返回的 Id 是字符串形式（long → string），详情能按它取回
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var providerId = await ReadIdAsync(response);
        Assert.True(providerId > 0);

        var detail = await GetDetailAsync(http, providerId);
        Assert.Equal(providerId.ToString(), detail.GetProperty("id").GetString());
        Assert.Equal("智谱", detail.GetProperty("name").GetString());
        Assert.Equal(2, detail.GetProperty("protocol").GetInt32());
        Assert.Equal("https://api.anthropic.com", detail.GetProperty("baseUrl").GetString());
        Assert.Equal("ak-test", detail.GetProperty("apiKey").GetString());
        // 多态设置以同样的形状返回
        var setting = detail.GetProperty("setting");
        Assert.Equal(2, setting.GetProperty("type").GetInt32());
        Assert.Equal("2023-06-01", setting.GetProperty("version").GetString());
        // 模型按名称排序
        var models = detail.GetProperty("models").EnumerateArray().ToList();
        Assert.Equal(["glm-5.3", "qwen"], models.Select(x => x.GetProperty("name").GetString()).ToList());
        Assert.Equal("旗舰", models[0].GetProperty("description").GetString());
        Assert.Empty(models[1].GetProperty("tagIds").EnumerateArray());
    }

    [Fact]
    public async Task Put_UpdatesConnectionAndSyncsModels_OnTheRoute()
    {
        using var http = _host.CreateClient();
        var providerId = await CreateProviderAsync(http, $"swap-{Guid.NewGuid():N}");

        // 修改：路由上只有提供方 Id，请求体带连接字段（含协议）与期望的完整模型列表
        var updated = await http.PutAsync($"/api/v1/ModelProviders/{providerId}", Json(new
        {
            name = "智谱（改）",
            protocol = 3,
            baseUrl = "https://vip.api.openai.com/v1",
            apiKey = "sk-new",
            setting = new { type = 3 },
            models = new[] { ModelBody("glm-5.3") },
        }));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var detail = await GetDetailAsync(http, providerId);
        Assert.Equal("智谱（改）", detail.GetProperty("name").GetString());
        Assert.Equal(3, detail.GetProperty("protocol").GetInt32());
        Assert.Equal("sk-new", detail.GetProperty("apiKey").GetString());
        Assert.Equal("glm-5.3", Assert.Single(detail.GetProperty("models").EnumerateArray()).GetProperty("name").GetString());

        // 删除后再删一次：提供方已不存在
        var deleted = await http.DeleteAsync($"/api/v1/ModelProviders/{providerId}");
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync($"/api/v1/ModelProviders/{providerId}")).StatusCode);

        var again = await http.DeleteAsync($"/api/v1/ModelProviders/{providerId}");
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Equal("模型提供方不存在。", await ReadMessageAsync(again));
    }

    [Fact]
    public async Task GetList_ReturnsEachProviderOnce()
    {
        using var http = _host.CreateClient();
        var providerId = await CreateProviderAsync(http, $"list-{Guid.NewGuid():N}");

        var response = await http.GetAsync("/api/v1/ModelProviders/List");
        response.EnsureSuccessStatusCode();

        var row = (await response.Content.ReadFromJsonAsync<JsonElement>())!.EnumerateArray()
            .Single(x => x.GetProperty("id").GetString() == providerId.ToString());
        Assert.Equal(1, row.GetProperty("protocol").GetInt32());
        Assert.Empty(row.GetProperty("modelNames").EnumerateArray());
    }

    [Fact]
    public async Task Post_InvalidBody_ReturnsProblemDetails400()
    {
        using var http = _host.CreateClient();

        var response = await http.PostAsync("/api/v1/ModelProviders", Json(ProviderBody("")));

        // FluentValidation 失败：400 + application/problem+json，错误按属性名分组
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"name\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Put_SettingOfAnotherProtocol_ReturnsProblemDetails400_AndChangesNothing()
    {
        using var http = _host.CreateClient();
        var providerId = await CreateProviderAsync(http, $"mismatch-{Guid.NewGuid():N}");

        // 声明 OpenAI 协议却带着 Anthropic 的设置：输入校验（FluentValidation）在进用例前拒绝
        var response = await http.PutAsync($"/api/v1/ModelProviders/{providerId}",
            Json(ProviderBody("智谱", protocol: 1, setting: new { type = 2 }, baseUrl: "https://changed")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("协议与设置类型不匹配", await response.Content.ReadAsStringAsync());

        // 被拒的请求什么都没改
        Assert.Equal("https://api.openai.com/v1", (await GetDetailAsync(http, providerId)).GetProperty("baseUrl").GetString());
    }

    [Fact]
    public async Task Post_WithNonexistentTag_ReturnsMessage400()
    {
        using var http = _host.CreateClient();
        var name = $"tag-{Guid.NewGuid():N}";

        var response = await http.PostAsync("/api/v1/ModelProviders", Json(new
        {
            name,
            protocol = 1,
            baseUrl = "https://api.openai.com/v1",
            apiKey = "sk-test",
            setting = new { type = 1 },
            models = new[] { ModelBody("glm-5.3", tagIds: [999999L]) },
        }));

        // 业务拒绝：400 + 消息文本以 JSON 字串给出（与 FluentValidation 的 ProblemDetails 是两种形状）
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("部分标签不存在，请刷新标签列表后再提交。", await ReadMessageAsync(response));
    }

    [Fact]
    public async Task Providers_AreIsolatedByTenantHeader()
    {
        using var tenant1 = _host.CreateClient(tenantId: 1);
        var providerId = await CreateProviderAsync(tenant1, $"tenant1-{Guid.NewGuid():N}");

        // 租户 0 看不到租户 1 的提供方：详情 404、修改 / 删除按「不存在」被拒
        using var tenant0 = _host.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await tenant0.GetAsync($"/api/v1/ModelProviders/{providerId}")).StatusCode);

        var update = await tenant0.PutAsync($"/api/v1/ModelProviders/{providerId}", Json(ProviderBody("抢占")));
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        Assert.Equal("模型提供方不存在。", await ReadMessageAsync(update));

        // 带上租户头就能看到
        Assert.StartsWith("tenant1-", (await GetDetailAsync(tenant1, providerId)).GetProperty("name").GetString());
    }

    [Fact]
    public async Task RemovedRoutes_NoLongerExist()
    {
        using var http = _host.CreateClient();

        // 接入点与模型目录的端点已删除：没有路由匹配（404），而不是命中某个处理方法
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/api/v1/ModelProviderEndpoints/List?modelProviderId=1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/api/v1/Models/PagedList")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.PostAsync("/api/v1/Models", Json(new { }))).StatusCode);
    }
}
