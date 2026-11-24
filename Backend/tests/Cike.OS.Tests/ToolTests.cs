using Cike.OS.Application.Contracts.Tools.Dtos;
using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;
using Cike.OS.Application.Tools.Commands;
using Cike.OS.Application.Tools.Queries;
using Cike.OS.Application.UserToolConfigurations.Commands;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.EntityFrameworkCore;
using Cike.OS.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using Xunit;

namespace Cike.OS.Tests;

public class ToolTests : IClassFixture<CikeOSTestHost>
{
    private readonly CikeOSTestHost _host;

    public ToolTests(CikeOSTestHost host)
    {
        _host = host;

        using var scope = _host.CreateScope();
        scope.ServiceProvider.GetRequiredService<FakeCurrentUser>().Id = "user-1";
    }

    private async Task<long> CreatePackageAsync()
    {
        var categoryId = await TestDataFactory.CreateCategoryAsync(_host, $"分类-{Guid.NewGuid():N}");
        return await TestDataFactory.CreatePackageAsync(_host, categoryId);
    }

    [Fact]
    public async Task Create_ThreeToolTypes_Then_Get_RoundTripsSettings()
    {
        var packageId = await CreatePackageAsync();

        var httpId = await TestDataFactory.CreateToolAsync(_host, packageId, type: ToolType.HttpMcp);
        var stdioId = await TestDataFactory.CreateToolAsync(_host, packageId, type: ToolType.StdioMcp);
        var skillId = await TestDataFactory.CreateToolAsync(_host, packageId, type: ToolType.Skill, skillDirectory: "skills/0/abc");

        var http = new GetToolQuery(httpId);
        await _host.PublishAsync(http);
        var httpSetting = Assert.IsType<HttpMcpToolSetting>(http.Result!.Setting);
        Assert.Equal(ToolType.HttpMcp, http.Result.Type);
        Assert.Equal("http://localhost:5000/mcp", httpSetting.Endpoint);
        Assert.Equal(TimeSpan.FromSeconds(30), httpSetting.ConnectionTimeout);

        var stdio = new GetToolQuery(stdioId);
        await _host.PublishAsync(stdio);
        var stdioSetting = Assert.IsType<StdioMcpToolSetting>(stdio.Result!.Setting);
        Assert.Equal("node", stdioSetting.Command);

        var skill = new GetToolQuery(skillId);
        await _host.PublishAsync(skill);
        var skillSetting = Assert.IsType<SkillToolSetting>(skill.Result!.Setting);
        Assert.Equal("skills/0/abc", skillSetting.Directory);
    }

    [Fact]
    public async Task Create_WithParametersAndHeaderReferences_RoundTrips()
    {
        var packageId = await CreatePackageAsync();

        var command = new CreateToolCommand(new CreateToolDto
        {
            Code = $"tool-{Guid.NewGuid():N}",
            Name = "带参数的工具",
            Type = ToolType.HttpMcp,
            ToolPackageId = packageId,
            Parameters =
            [
                new ToolParameterDefinition { Code = "ak", Name = "Access key", Required = true, Sensitive = true },
            ],
            Setting = new HttpMcpToolSetting
            {
                Name = "http",
                Endpoint = "http://localhost:5000/mcp",
                TransportMode = HttpTransportMode.Sse,
                ConnectionTimeout = TimeSpan.FromSeconds(15),
                AdditionalHeaders = new Dictionary<string, ToolValue>
                {
                    ["X-Api-Key"] = ToolValue.FromParameter("ak"),
                    ["X-Static"] = ToolValue.FromLiteral("1"),
                },
            },
        });
        await _host.PublishAsync(command);

        var query = new GetToolQuery(command.Id);
        await _host.PublishAsync(query);

        var parameter = Assert.Single(query.Result!.Parameters);
        Assert.Equal("ak", parameter.Code);
        Assert.True(parameter.Sensitive);

        var setting = Assert.IsType<HttpMcpToolSetting>(query.Result.Setting);
        Assert.Equal(HttpTransportMode.Sse, setting.TransportMode);
        Assert.Equal(TimeSpan.FromSeconds(15), setting.ConnectionTimeout);
        Assert.Equal(ToolValueType.ParameterReference, setting.AdditionalHeaders!["X-Api-Key"].Type);
        Assert.Equal("ak", setting.AdditionalHeaders["X-Api-Key"].Value);
    }

    [Fact]
    public async Task Create_WithUnknownPackage_Throws()
    {
        var command = new CreateToolCommand(new CreateToolDto
        {
            Code = $"tool-{Guid.NewGuid():N}",
            Name = "孤儿工具",
            Type = ToolType.HttpMcp,
            ToolPackageId = 123456789,
            Setting = new HttpMcpToolSetting { Endpoint = "http://localhost/mcp" },
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(command));
    }

    [Fact]
    public async Task Create_WithDuplicateCode_Throws()
    {
        var packageId = await CreatePackageAsync();
        var code = $"dup-tool-{Guid.NewGuid():N}";
        await TestDataFactory.CreateToolAsync(_host, packageId, code);

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => TestDataFactory.CreateToolAsync(_host, packageId, code));
    }

    [Fact]
    public async Task Create_WithMismatchedSettingType_Throws_AggregateInvariant()
    {
        // 绕过 Validator 直接发 Command：聚合根工厂必须自己拦住「形态与设置不匹配」
        var packageId = await CreatePackageAsync();
        var command = new CreateToolCommand(new CreateToolDto
        {
            Code = $"tool-{Guid.NewGuid():N}",
            Name = "错配",
            Type = ToolType.Skill,
            ToolPackageId = packageId,
            Setting = new HttpMcpToolSetting { Endpoint = "http://localhost/mcp" },
        });

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(command));
        Assert.Contains("不匹配", ex.Message);
    }

    [Fact]
    public async Task Create_WithDuplicateParameterCodes_Throws_AggregateInvariant()
    {
        var packageId = await CreatePackageAsync();
        var command = new CreateToolCommand(new CreateToolDto
        {
            Code = $"tool-{Guid.NewGuid():N}",
            Name = "重复参数",
            Type = ToolType.HttpMcp,
            ToolPackageId = packageId,
            Parameters = [new ToolParameterDefinition { Code = "dup", Name = "一" }, new ToolParameterDefinition { Code = "dup", Name = "二" }],
            Setting = new HttpMcpToolSetting { Endpoint = "http://localhost/mcp" },
        });

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(command));
        Assert.Contains("重复", ex.Message);
    }

    [Fact]
    public async Task Update_SkillToolWithOtherSetting_Throws_TypeIsImmutable_NotInvalidCast()
    {
        // 技能工具收到 HTTP 设置：形态不可变，必须是业务异常（400），不能是 InvalidCastException（500）
        var packageId = await CreatePackageAsync();
        var toolId = await TestDataFactory.CreateToolAsync(_host, packageId, type: ToolType.Skill, skillDirectory: "skills/0/not-exist");

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(new UpdateToolCommand(toolId, new UpdateToolDto
        {
            Code = $"tool-{Guid.NewGuid():N}",
            Name = "改名",
            ToolPackageId = packageId,
            Setting = new HttpMcpToolSetting { Endpoint = "http://localhost/mcp" },
        })));
        Assert.Contains("不可修改", ex.Message);
    }

    [Fact]
    public async Task GetList_ReturnsOnlyToolsOfPackage_OrderedByCode()
    {
        var packageId = await CreatePackageAsync();
        var otherPackageId = await CreatePackageAsync();
        await TestDataFactory.CreateToolAsync(_host, packageId, code: "zzz");
        await TestDataFactory.CreateToolAsync(_host, packageId, code: "aaa");
        await TestDataFactory.CreateToolAsync(_host, otherPackageId, code: "other");

        var query = new GetToolListQuery(packageId);
        await _host.PublishAsync(query);

        Assert.Equal(["aaa", "zzz"], query.Result!.Select(x => x.Code));
    }

    [Fact]
    public async Task Update_ChangesMutableFields_KeepsType()
    {
        var packageId = await CreatePackageAsync();
        var toolId = await TestDataFactory.CreateToolAsync(_host, packageId, type: ToolType.StdioMcp, code: "before");

        await _host.PublishAsync(new UpdateToolCommand(toolId, new UpdateToolDto {
            Code = "after",
            Name = "改名后",
            ToolPackageId = packageId,
            Setting = new StdioMcpToolSetting { Name = "stdio", Command = "python", Arguments = ["-m", "server"] },
        }));

        var query = new GetToolQuery(toolId);
        await _host.PublishAsync(query);

        Assert.Equal("after", query.Result!.Code);
        Assert.Equal(ToolType.StdioMcp, query.Result.Type);
        var setting = Assert.IsType<StdioMcpToolSetting>(query.Result.Setting);
        Assert.Equal("python", setting.Command);
        Assert.Equal(["-m", "server"], setting.Arguments);
    }

    [Fact]
    public async Task Update_WithSettingOfAnotherType_Throws_TypeIsImmutable()
    {
        var packageId = await CreatePackageAsync();
        var toolId = await TestDataFactory.CreateToolAsync(_host, packageId, code: "immutable");

        // 用 Stdio 设置去更新一个 Http MCP 工具：等价于改形态，聚合根必须拒绝
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(new UpdateToolCommand(toolId, new UpdateToolDto {
            Code = "immutable",
            Name = "改名",
            ToolPackageId = packageId,
            Setting = new StdioMcpToolSetting { Name = "stdio", Command = "python" },
        })));
        Assert.Contains("不可修改", ex.Message);

        var query = new GetToolQuery(toolId);
        await _host.PublishAsync(query);
        Assert.Equal(ToolType.HttpMcp, query.Result!.Type);
        Assert.IsType<HttpMcpToolSetting>(query.Result.Setting);
    }

    [Fact]
    public async Task Delete_CascadesUserToolConfigurations()
    {
        var packageId = await CreatePackageAsync();
        var toolId = await TestDataFactory.CreateToolAsync(_host, packageId);

        await _host.PublishAsync(new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = toolId,
            Values = new Dictionary<string, string>(),
        }));

        await _host.PublishAsync(new DeleteToolCommand(toolId));

        using var scope = _host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();
        Assert.False(await dbContext.UserToolConfigurations.IgnoreQueryFilters().AnyAsync(x => x.ToolId == toolId && !x.IsDeleted));
        Assert.True(await dbContext.UserToolConfigurations.IgnoreQueryFilters().AnyAsync(x => x.ToolId == toolId && x.IsDeleted));
    }

    [Fact]
    public async Task Delete_ReferencedByAgentTool_Throws()
    {
        var packageId = await CreatePackageAsync();
        var toolId = await TestDataFactory.CreateToolAsync(_host, packageId);

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();
            dbContext.Add(new AgentToolPackage
            {
                AgentId = 1,
                ToolPackageId = packageId,
                AgentTools = [new AgentTool { ToolId = toolId }],
            });
            await dbContext.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(new DeleteToolCommand(toolId)));

        var query = new GetToolQuery(toolId);
        await _host.PublishAsync(query);
        Assert.NotNull(query.Result);
    }

    [Fact]
    public async Task Delete_NotReferenced_SoftDeletes()
    {
        var packageId = await CreatePackageAsync();
        var toolId = await TestDataFactory.CreateToolAsync(_host, packageId);

        await _host.PublishAsync(new DeleteToolCommand(toolId));

        var query = new GetToolQuery(toolId);
        await _host.PublishAsync(query);
        Assert.Null(query.Result);
    }
}
