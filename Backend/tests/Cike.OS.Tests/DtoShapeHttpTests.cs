using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cike.OS.Application.Tags.Commands;
using Cike.OS.Application.ToolPackageCategories.Commands;
using Cike.OS.Application.ToolPackages.Commands;
using Cike.OS.Application.Tools.Commands;
using Cike.OS.Application.UserToolConfigurations.Commands;
using Cike.OS.Application.Contracts.Tags.Dtos;
using Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;
using Cike.OS.Application.Contracts.ToolPackages.Dtos;
using Cike.OS.Application.Contracts.Tools.Dtos;
using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.Tests.Infrastructure;
using Xunit;

namespace Cike.OS.Tests;

/// <summary>
/// 输出 DTO 收窄后的 HTTP 形状：列表行只带消费方用到的字段（不再夹带 Readme / Setting / 参数定义），
/// 详情接口返回编辑回填需要的全部东西；路由与查询串参数名不变。
/// </summary>
public class DtoShapeHttpTests : IClassFixture<ServiceOpenTestHost>
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly ServiceOpenTestHost _host;

    public DtoShapeHttpTests(ServiceOpenTestHost host)
    {
        _host = host;
        _host.SetCurrentUserId($"shape-{Guid.NewGuid():N}");
    }

    private static StringContent Json(object body) => new(JsonSerializer.Serialize(body, Web), Encoding.UTF8, "application/json");

    private static async Task<JsonElement> GetJsonAsync(HttpClient http, string url)
    {
        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>一个分类 / 工具包 / 带一个参数的工具，返回三个 Id。</summary>
    private async Task<(long CategoryId, long PackageId, long ToolId, string CategoryName, string PackageName)> CreateToolAsync()
    {
        var categoryName = $"分类-{Guid.NewGuid():N}";
        var packageName = $"形状测试包-{Guid.NewGuid():N}";
        var category = new CreateToolPackageCategoryCommand(new CreateToolPackageCategoryDto { Name = categoryName });
        await _host.PublishAsync(category);
        var package = new CreateToolPackageCommand(new CreateToolPackageDto
        {
            Code = $"pkg-{Guid.NewGuid():N}",
            Name = packageName,
            Description = "形状测试描述",
            Readme = "# readme",
            ToolPackageCategoryId = category.Id,
            Status = ToolPackageStatus.Published,
        });
        await _host.PublishAsync(package);
        var tool = new CreateToolCommand(new CreateToolDto
        {
            Code = $"tool-{Guid.NewGuid():N}",
            Name = $"形状工具-{Guid.NewGuid():N}",
            Description = "形状测试",
            Type = ToolType.HttpMcp,
            ToolPackageId = package.Id,
            Parameters = [new ToolParameterDefinition { Code = "ak", Name = "Access key", Required = true, Sensitive = true }],
            Setting = new HttpMcpToolSetting { Endpoint = "http://localhost/mcp" },
        });
        await _host.PublishAsync(tool);
        return (category.Id, package.Id, tool.Id, categoryName, packageName);
    }

    [Fact]
    public async Task Tool_List_HasParameterCountOnly_DetailHasParametersAndSetting()
    {
        var (_, packageId, toolId, _, _) = await CreateToolAsync();
        using var http = _host.CreateClient();

        var rows = await http.GetFromJsonAsync<List<JsonElement>>($"/api/v1/Tools/List?toolPackageId={packageId}");
        var row = Assert.Single(rows!, x => x.GetProperty("id").GetString() == toolId.ToString());
        Assert.Equal(1, row.GetProperty("parameterCount").GetInt32());
        Assert.False(row.TryGetProperty("setting", out _));
        Assert.False(row.TryGetProperty("toolPackageId", out _));
        Assert.False(row.TryGetProperty("parameters", out _));

        var detail = await GetJsonAsync(http, $"/api/v1/Tools/{toolId}");
        Assert.Equal(packageId.ToString(), detail.GetProperty("toolPackageId").GetString());
        Assert.Single(detail.GetProperty("parameters").EnumerateArray());
        Assert.True(detail.TryGetProperty("setting", out _));
    }

    [Fact]
    public async Task ToolPackage_PagedListHasNoReadme_DetailHasReadme()
    {
        var (_, packageId, _, _, packageName) = await CreateToolAsync();
        using var http = _host.CreateClient();

        var page = await GetJsonAsync(http, $"/api/v1/ToolPackages/PagedList?Keyword={Uri.EscapeDataString(packageName)}&Page=1&PageSize=10");
        var row = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal(packageId.ToString(), row.GetProperty("id").GetString());
        Assert.Equal(packageName, row.GetProperty("name").GetString());
        Assert.False(row.TryGetProperty("readme", out _));
        Assert.False(row.TryGetProperty("toolPackageCategoryId", out _));

        var detail = await GetJsonAsync(http, $"/api/v1/ToolPackages/{packageId}");
        Assert.Equal("# readme", detail.GetProperty("readme").GetString());
        Assert.True(detail.TryGetProperty("toolPackageCategoryId", out _));
    }

    [Fact]
    public async Task UserToolConfiguration_Get_ReturnsToolNameParametersValues_Or404()
    {
        var (_, _, toolId, _, _) = await CreateToolAsync();
        using var http = _host.CreateClient();

        // 当前用户尚未配置：404
        var missing = await http.GetAsync($"/api/v1/UserToolConfigurations/{toolId}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var saved = await http.PostAsync("/api/v1/UserToolConfigurations/Save", Json(new
        {
            toolId,
            values = new Dictionary<string, string> { ["ak"] = "sk-shape" },
        }));
        saved.EnsureSuccessStatusCode();

        var detail = await GetJsonAsync(http, $"/api/v1/UserToolConfigurations/{toolId}");
        Assert.Equal(toolId.ToString(), detail.GetProperty("toolId").GetString());
        Assert.Equal("sk-shape", detail.GetProperty("values").GetProperty("ak").GetString());
        Assert.Equal("ak", Assert.Single(detail.GetProperty("parameters").EnumerateArray()).GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(detail.GetProperty("toolName").GetString()));

        // 卡片不带工具描述，候选不带配置时间
        var page = await GetJsonAsync(http, "/api/v1/UserToolConfigurations/PagedList?Page=1&PageSize=100");
        var card = Assert.Single(page.GetProperty("items").EnumerateArray(), x => x.GetProperty("toolId").GetString() == toolId.ToString());
        Assert.False(card.TryGetProperty("toolDescription", out _));
        var options = await http.GetFromJsonAsync<List<JsonElement>>("/api/v1/UserToolConfigurations/ToolOptions");
        var option = Assert.Single(options!, x => x.GetProperty("toolId").GetString() == toolId.ToString());
        Assert.False(option.TryGetProperty("configuredAt", out _));
        Assert.True(option.GetProperty("configured").GetBoolean());
    }

    [Fact]
    public async Task Tag_And_Category_GetById_ReturnDetail_UnknownId404()
    {
        var tag = new CreateTagCommand(new CreateTagDto { Key = $"shape-{Guid.NewGuid():N}", Name = "形状标签" });
        await _host.PublishAsync(tag);
        var (_, packageId, _, categoryName, _) = await CreateToolAsync();
        using var http = _host.CreateClient();
        var packageDetail = await GetJsonAsync(http, $"/api/v1/ToolPackages/{packageId}");
        var categoryId = packageDetail.GetProperty("toolPackageCategoryId").GetString();

        var tagDetail = await GetJsonAsync(http, $"/api/v1/Tags/{tag.Id}");
        Assert.Equal("形状标签", tagDetail.GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/api/v1/Tags/999999")).StatusCode);

        var categoryDetail = await GetJsonAsync(http, $"/api/v1/ToolPackageCategories/{categoryId}");
        Assert.Equal(categoryName, categoryDetail.GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/api/v1/ToolPackageCategories/999999")).StatusCode);

        // 列表行与详情的 schema 名不同（Item / Detail），字段集合一致的小实体各自独立
        var tags = await http.GetFromJsonAsync<List<JsonElement>>("/api/v1/Tags/List");
        Assert.Single(tags!, x => x.GetProperty("id").GetString() == tag.Id.ToString());
    }
}
