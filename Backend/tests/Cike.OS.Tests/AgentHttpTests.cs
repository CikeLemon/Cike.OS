using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cike.OS.Tests.Infrastructure;
using Xunit;

namespace Cike.OS.Tests;

/// <summary>
/// Agent 六条约定路由的 HTTP 形状：动词、路径、含 M.E.AI 多态（$type 判别）工具模式的请求体与响应体、校验与业务拒绝两种 400、详情的 404、租户隔离。
/// 业务规则（重名、工具同步、引用校验）由 AgentTests 经命令 / 查询接缝覆盖，这里不重复。
/// </summary>
public class AgentHttpTests : IClassFixture<ServiceOpenTestHost>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly ServiceOpenTestHost _host;

    public AgentHttpTests(ServiceOpenTestHost host)
    {
        _host = host;
    }

    private static StringContent Json(object body) => new(JsonSerializer.Serialize(body, Web), Encoding.UTF8, "application/json");

    private static object Tool(int type, long targetId) => new { type, targetId };

    /// <summary>M.E.AI 工具模式的 wire 形状：判别键 $type，取值 none / auto / required。</summary>
    private static object ToolMode(string type) => new Dictionary<string, object> { ["$type"] = type };

    private static object AgentBody(string name, object[]? tools = null, object? options = null) => new
    {
        name,
        description = "描述",
        instructions = "你是一名客服。",
        options = options ?? new { },
        tools = tools ?? [],
    };

    private static async Task<long> ReadIdAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return long.Parse(JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync(), Web)!);
    }

    private static async Task<JsonElement> GetDetailAsync(HttpClient http, long id)
    {
        var response = await http.GetAsync($"/api/v1/Agents/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string> ReadMessageAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync(), Web)!;

    [Fact]
    public async Task GetDetail_OfUnknownAgent_Returns404()
    {
        using var http = _host.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/api/v1/Agents/999999")).StatusCode);
    }

    [Fact]
    public async Task Post_CreatesAgent_ThenGetDetailRoundTripsToolModeAndReasoning()
    {
        using var http = _host.CreateClient();

        // 工具模式是 M.E.AI 的多态对象：判别键 $type，可以不在对象首位
        var response = await http.PostAsync("/api/v1/Agents", Json(AgentBody($"http-{Guid.NewGuid():N}", options: new
        {
            temperature = 0.7,
            toolMode = ToolMode("auto"),
            reasoning = new { effort = 2, output = 1 },
            responseFormat = new { type = 2, jsonSchemaName = "answer", jsonSchemaDescriptor = "{\"type\":\"object\"}" },
        })));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var id = await ReadIdAsync(response);
        Assert.True(id > 0);

        var detail = await GetDetailAsync(http, id);
        Assert.Equal(id.ToString(), detail.GetProperty("id").GetString());
        Assert.Equal(0.7, detail.GetProperty("options").GetProperty("temperature").GetDouble());
        Assert.Equal("auto", detail.GetProperty("options").GetProperty("toolMode").GetProperty("$type").GetString());
        Assert.Equal(2, detail.GetProperty("options").GetProperty("reasoning").GetProperty("effort").GetInt32());
        Assert.Equal("answer", detail.GetProperty("options").GetProperty("responseFormat").GetProperty("jsonSchemaName").GetString());
        Assert.Empty(detail.GetProperty("tools").EnumerateArray());

        // 按同样的形状再发回去也能更新：$type 多态在写入侧同样成立
        var updated = await http.PutAsync($"/api/v1/Agents/{id}", Json(AgentBody($"http-{Guid.NewGuid():N}", options: new
        {
            toolMode = ToolMode("required"),
        })));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("required", (await GetDetailAsync(http, id)).GetProperty("options").GetProperty("toolMode").GetProperty("$type").GetString());
    }

    [Fact]
    public async Task Post_WithTools_ThenPutSyncsAndDeleteRemoves()
    {
        using var http = _host.CreateClient();
        // 整包绑定的包与单选工具所在的包不能是同一个（后端会拒），所以造两个包
        var packageId = await CreatePackageAsync();
        var toolId = await CreateToolAsync(await CreatePackageAsync());

        var response = await http.PostAsync("/api/v1/Agents", Json(AgentBody($"tools-{Guid.NewGuid():N}",
            tools: [Tool(1, packageId), Tool(2, toolId)])));
        var id = await ReadIdAsync(response);

        var tools = (await GetDetailAsync(http, id)).GetProperty("tools").EnumerateArray().ToList();
        Assert.Equal([(1, packageId.ToString()), (2, toolId.ToString())], tools.Select(x => (x.GetProperty("type").GetInt32(), x.GetProperty("targetId").GetString())).ToList());

        var updated = await http.PutAsync($"/api/v1/Agents/{id}", Json(AgentBody($"tools-{Guid.NewGuid():N}", tools: [Tool(2, toolId)])));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var row = Assert.Single((await GetDetailAsync(http, id)).GetProperty("tools").EnumerateArray());
        Assert.Equal(2, row.GetProperty("type").GetInt32());

        Assert.Equal(HttpStatusCode.OK, (await http.DeleteAsync($"/api/v1/Agents/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync($"/api/v1/Agents/{id}")).StatusCode);

        var again = await http.DeleteAsync($"/api/v1/Agents/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Equal("Agent 不存在。", await ReadMessageAsync(again));
    }

    [Fact]
    public async Task GetPagedList_And_ToolOptions_AreMapped()
    {
        using var http = _host.CreateClient();
        var suffix = Guid.NewGuid().ToString("N");
        var id = await ReadIdAsync(await http.PostAsync("/api/v1/Agents", Json(AgentBody($"paged-{suffix}"))));

        var paged = await http.GetAsync($"/api/v1/Agents/PagedList?keyword=paged-{suffix}&Page=1&PageSize=10&Sorting=UpdatedAt%20desc");
        paged.EnsureSuccessStatusCode();
        var page = await paged.Content.ReadFromJsonAsync<JsonElement>();
        var row = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal(id.ToString(), row.GetProperty("id").GetString());

        var options = await http.GetAsync("/api/v1/Agents/ToolOptions");
        options.EnsureSuccessStatusCode();
        Assert.Equal(JsonValueKind.Array, (await options.Content.ReadFromJsonAsync<JsonElement>()).ValueKind);
    }

    private static readonly StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;

    [Fact]
    public async Task Post_InvalidBody_ReturnsProblemDetails400()
    {
        using var http = _host.CreateClient();

        // 名称、指令为空，模型选项越界，工具条目非法：FluentValidation 失败 → 400 + application/problem+json，错误按属性名分组
        var response = await http.PostAsync("/api/v1/Agents", Json(new
        {
            name = "",
            description = (string?)null,
            instructions = "",
            options = new { temperature = 3, toolMode = ToolMode("auto"), stopSequences = new[] { "" } },
            tools = new[] { new { type = 9, targetId = 0 } },
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"name\"", body);
        Assert.Contains("\"instructions\"", body);
        Assert.Contains("Temperature", body, IgnoreCase);
        Assert.Contains("StopSequences", body, IgnoreCase);
        Assert.Contains("tools[0]", body, IgnoreCase);
    }

    [Fact]
    public async Task Post_ToolModeWithUnknownDiscriminator_Returns400()
    {
        using var http = _host.CreateClient();

        // 不在 none / auto / required 之内的 $type：STJ 反序列化阶段就拒绝
        var response = await http.PostAsync("/api/v1/Agents", Json(AgentBody($"bad-{Guid.NewGuid():N}", options: new
        {
            toolMode = ToolMode("whatever"),
        })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_DuplicateName_Or_UnknownPackage_ReturnsMessage400()
    {
        using var http = _host.CreateClient();
        var name = $"dup-{Guid.NewGuid():N}";
        await ReadIdAsync(await http.PostAsync("/api/v1/Agents", Json(AgentBody(name))));

        var duplicate = await http.PostAsync("/api/v1/Agents", Json(AgentBody(name)));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Equal("application/json", duplicate.Content.Headers.ContentType?.MediaType);
        Assert.Equal($"Agent「{name}」已存在。", await ReadMessageAsync(duplicate));

        var unknown = await http.PostAsync("/api/v1/Agents", Json(AgentBody($"unknown-{Guid.NewGuid():N}", tools: [Tool(1, 999999)])));
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal("部分工具包不存在，请刷新后重新选择。", await ReadMessageAsync(unknown));
    }

    [Fact]
    public async Task Agents_AreIsolatedByTenantHeader()
    {
        using var tenant1 = _host.CreateClient(tenantId: 1);
        var id = await ReadIdAsync(await tenant1.PostAsync("/api/v1/Agents", Json(AgentBody($"tenant1-{Guid.NewGuid():N}"))));

        using var tenant0 = _host.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await tenant0.GetAsync($"/api/v1/Agents/{id}")).StatusCode);

        var update = await tenant0.PutAsync($"/api/v1/Agents/{id}", Json(AgentBody("抢占")));
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        Assert.Equal("Agent 不存在。", await ReadMessageAsync(update));

        Assert.Equal(id.ToString(), (await GetDetailAsync(tenant1, id)).GetProperty("id").GetString());
    }

    /// <summary>造一个工具包（经命令接缝，不走 HTTP——分类 / 工具包的路由有自己的测试）。</summary>
    private async Task<long> CreatePackageAsync()
    {
        var category = new Cike.OS.Application.ToolPackageCategories.Commands.CreateToolPackageCategoryCommand(
            new Cike.OS.Application.Contracts.ToolPackageCategories.Dtos.CreateToolPackageCategoryDto { Name = $"分类-{Guid.NewGuid():N}" });
        await _host.PublishAsync(category);
        var package = new Cike.OS.Application.ToolPackages.Commands.CreateToolPackageCommand(
            new Cike.OS.Application.Contracts.ToolPackages.Dtos.CreateToolPackageDto
            {
                Code = $"pkg-{Guid.NewGuid():N}",
                Name = "HTTP 测试包",
                ToolPackageCategoryId = category.Id,
                Status = Cike.OS.Domain.Shared.Enums.ToolPackageStatus.Published,
            });
        await _host.PublishAsync(package);
        return package.Id;
    }

    private async Task<long> CreateToolAsync(long packageId)
    {
        var tool = new Cike.OS.Application.Tools.Commands.CreateToolCommand(
            new Cike.OS.Application.Contracts.Tools.Dtos.CreateToolDto
            {
                Code = $"tool-{Guid.NewGuid():N}",
                Name = "HTTP 测试工具",
                Type = Cike.OS.Domain.Shared.Enums.ToolType.HttpMcp,
                ToolPackageId = packageId,
                Setting = new Cike.OS.Domain.Shared.ValueObjects.HttpMcpToolSetting { Endpoint = "http://localhost/mcp" },
            });
        await _host.PublishAsync(tool);
        return tool.Id;
    }
}
