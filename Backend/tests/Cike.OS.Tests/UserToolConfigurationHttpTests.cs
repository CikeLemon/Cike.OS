using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;
using Cike.OS.Application.Contracts.Tools.Dtos;
using Cike.OS.Application.Contracts.ToolPackages.Dtos;
using Cike.OS.Application.ToolPackageCategories.Commands;
using Cike.OS.Application.ToolPackages.Commands;
using Cike.OS.Application.Tools.Commands;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.Tests.Infrastructure;
using Xunit;

namespace Cike.OS.Tests;

/// <summary>用户工具配置三条新路由的 HTTP 形状（分页 / 工具选项 / 批量保存），以及校验与业务拒绝两种 400。</summary>
public class UserToolConfigurationHttpTests : IClassFixture<ServiceOpenTestHost>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly ServiceOpenTestHost _host;

    public UserToolConfigurationHttpTests(ServiceOpenTestHost host)
    {
        _host = host;
        // 每个用例一个独立的用户，避免共享宿主里其它用例的配置混进来
        _host.SetCurrentUserId($"http-user-{Guid.NewGuid():N}");
    }

    private static StringContent Json(object body) => new(JsonSerializer.Serialize(body, Web), Encoding.UTF8, "application/json");

    private static async Task<string> ReadMessageAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<string>(await response.Content.ReadAsStringAsync(), Web)!;

    private async Task<long> CreateToolWithParametersAsync(string name)
    {
        var category = new CreateToolPackageCategoryCommand(new CreateToolPackageCategoryDto { Name = $"分类-{Guid.NewGuid():N}" });
        await _host.PublishAsync(category);
        var package = new CreateToolPackageCommand(new CreateToolPackageDto
        {
            Code = $"pkg-{Guid.NewGuid():N}",
            Name = "HTTP 测试包",
            ToolPackageCategoryId = category.Id,
            Status = ToolPackageStatus.Published,
        });
        await _host.PublishAsync(package);
        var tool = new CreateToolCommand(new CreateToolDto
        {
            Code = $"tool-{Guid.NewGuid():N}",
            Name = name,
            Type = ToolType.HttpMcp,
            ToolPackageId = package.Id,
            Parameters = [new ToolParameterDefinition { Code = "ak", Name = "Access key", Required = true, Sensitive = true }],
            Setting = new HttpMcpToolSetting { Endpoint = "http://localhost/mcp" },
        });
        await _host.PublishAsync(tool);
        return tool.Id;
    }

    [Fact]
    public async Task GetPagedList_WithoutUser_ReturnsMessage400()
    {
        _host.SetCurrentUserId(null);
        using var http = _host.CreateClient();

        var response = await http.GetAsync("/api/v1/UserToolConfigurations/PagedList?Page=1&PageSize=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Current user id is required.", await ReadMessageAsync(response));
    }

    [Fact]
    public async Task SaveBatch_ThenPagedListAndToolOptions_SeeTheConfiguration()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var toolId = await CreateToolWithParametersAsync($"天气-{suffix}");
        var pendingId = await CreateToolWithParametersAsync($"待配置-{suffix}");
        using var http = _host.CreateClient();

        var saved = await http.PostAsync("/api/v1/UserToolConfigurations/SaveBatch", Json(new[]
        {
            new { toolId, values = new Dictionary<string, string> { ["ak"] = "sk-batch" } },
        }));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var paged = await http.GetAsync($"/api/v1/UserToolConfigurations/PagedList?keyword=天气-{suffix}&Page=1&PageSize=10&Sorting=UpdatedAt%20desc");
        paged.EnsureSuccessStatusCode();
        var card = Assert.Single((await paged.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray());
        Assert.Equal(toolId.ToString(), card.GetProperty("toolId").GetString());
        Assert.Equal($"天气-{suffix}", card.GetProperty("toolName").GetString());
        Assert.Equal("sk-batch", card.GetProperty("values").GetProperty("ak").GetString());
        Assert.Equal("HTTP 测试包", card.GetProperty("toolPackageName").GetString());
        Assert.Single(card.GetProperty("parameters").EnumerateArray());

        var options = await http.GetAsync("/api/v1/UserToolConfigurations/ToolOptions");
        options.EnsureSuccessStatusCode();
        var rows = (await options.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        Assert.True(rows.Single(x => x.GetProperty("toolId").GetString() == toolId.ToString()).GetProperty("configured").GetBoolean());
        Assert.False(rows.Single(x => x.GetProperty("toolId").GetString() == pendingId.ToString()).GetProperty("configured").GetBoolean());
    }

    [Fact]
    public async Task SaveBatch_EmptyOrDuplicate_ReturnsProblemDetails400()
    {
        using var http = _host.CreateClient();

        var empty = await http.PostAsync("/api/v1/UserToolConfigurations/SaveBatch", Json(Array.Empty<object>()));
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal("application/problem+json", empty.Content.Headers.ContentType?.MediaType);

        var duplicate = await http.PostAsync("/api/v1/UserToolConfigurations/SaveBatch", Json(new[]
        {
            new { toolId = 1, values = new Dictionary<string, string>() },
            new { toolId = 1, values = new Dictionary<string, string>() },
        }));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SaveBatch_MissingRequiredParameter_ReturnsMessage400_AndSavesNothing()
    {
        var toolId = await CreateToolWithParametersAsync($"缺必填-{Guid.NewGuid():N}");
        using var http = _host.CreateClient();

        var response = await http.PostAsync("/api/v1/UserToolConfigurations/SaveBatch", Json(new[]
        {
            new { toolId, values = new Dictionary<string, string>() },
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("required", await ReadMessageAsync(response), StringComparison.OrdinalIgnoreCase);

        // 被拒的请求什么都没存：工具选项里它仍未配置
        var options = await http.GetAsync("/api/v1/UserToolConfigurations/ToolOptions");
        var row = (await options.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()
            .Single(x => x.GetProperty("toolId").GetString() == toolId.ToString());
        Assert.False(row.GetProperty("configured").GetBoolean());
    }
}
