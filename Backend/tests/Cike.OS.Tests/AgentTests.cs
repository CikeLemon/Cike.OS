using Cike.MultiTenancy;
using Cike.Contracts.EntityDtos;
using Cike.OS.Application.Agents.Commands;
using Cike.OS.Application.Agents.Queries;
using Cike.OS.Application.Contracts.Agents.Dtos;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.EntityFrameworkCore;
using Cike.OS.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cike.OS.Tests;

/// <summary>
/// Agent 业务规则：从发布 Command / Query 这一点进入（名称唯一、工具整体同步、引用校验、级联软删、租户隔离、工具选项）。
/// 输入校验（FluentValidation 只在 HTTP 管线跑）由 AgentHttpTests 覆盖。
/// </summary>
public class AgentTests : IClassFixture<CikeOSTestHost>
{
    private readonly CikeOSTestHost _host;

    public AgentTests(CikeOSTestHost host)
    {
        _host = host;
    }

    /// <summary>切换环境租户（AsyncLocal 随异步链路流动，PublishAsync 内的租户过滤都会跟随）。</summary>
    private async Task WithTenantAsync(long tenantId, Func<Task> action)
    {
        using var scope = _host.CreateScope();
        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        using var _ = currentTenant.Change(tenantId);
        await action();
    }

    /// <summary>一个分类下两个工具包，各带一个工具；返回 (packageId, toolId, otherPackageId, otherToolId)。</summary>
    private async Task<(long PackageId, long ToolId, long OtherPackageId, long OtherToolId)> CreatePackagesWithToolsAsync()
    {
        var categoryId = await TestDataFactory.CreateCategoryAsync(_host, $"分类-{Guid.NewGuid():N}");
        var packageId = await TestDataFactory.CreatePackageAsync(_host, categoryId);
        var toolId = await TestDataFactory.CreateToolAsync(_host, packageId);
        var otherPackageId = await TestDataFactory.CreatePackageAsync(_host, categoryId);
        var otherToolId = await TestDataFactory.CreateToolAsync(_host, otherPackageId);
        return (packageId, toolId, otherPackageId, otherToolId);
    }

    private static GetAgentPagedListQuery PagedQuery(string? keyword) =>
        new(keyword, new PagedAndSortedResultRequest { Page = 1, PageSize = 10 });

    [Fact]
    public async Task Create_WithWholePackageAndSingleTool_ThenGet_ReturnsBothKindsOfRows()
    {
        // 整包绑定一个包，同时单选另一个包里的工具（同一包不能既整包又单选）
        var (packageId, _, _, otherToolId) = await CreatePackagesWithToolsAsync();

        var id = await TestDataFactory.CreateAgentAsync(_host, TestDataFactory.Agent(
            "客服助手", "回答问题", tools: [TestDataFactory.AgentTool(AgentToolType.ToolPackage, packageId), TestDataFactory.AgentTool(AgentToolType.Tool, otherToolId)]));

        var agent = await TestDataFactory.GetAgentAsync(_host, id);
        Assert.NotNull(agent);
        Assert.Equal("客服助手", agent!.Name);
        Assert.Equal("回答问题", agent.Description);
        Assert.Equal("You are a helpful assistant.", agent.Instructions);
        // 工具按类型、目标排序：整包行在前
        Assert.Equal([(AgentToolType.ToolPackage, packageId), (AgentToolType.Tool, otherToolId)], agent.Tools.Select(x => (x.Type, x.TargetId)).ToList());
    }

    [Fact]
    public async Task Create_WithoutTools_ReturnsEmptyTools()
    {
        var id = await TestDataFactory.CreateAgentAsync(_host);

        var agent = await TestDataFactory.GetAgentAsync(_host, id);
        Assert.NotNull(agent);
        Assert.Empty(agent!.Tools);
    }

    [Fact]
    public async Task Create_DuplicateNameInSameTenant_Throws()
    {
        var name = $"dup-{Guid.NewGuid():N}";
        await TestDataFactory.CreateAgentAsync(_host, TestDataFactory.Agent(name));

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => TestDataFactory.CreateAgentAsync(_host, TestDataFactory.Agent(name)));
        Assert.Contains("已存在", ex.Message);
    }

    [Fact]
    public async Task Create_SameNameInOtherTenant_Succeeds()
    {
        var name = $"tenant-{Guid.NewGuid():N}";
        await TestDataFactory.CreateAgentAsync(_host, TestDataFactory.Agent(name));

        await WithTenantAsync(1, async () =>
        {
            var id = await TestDataFactory.CreateAgentAsync(_host, TestDataFactory.Agent(name));
            Assert.True(id > 0);
        });
    }

    [Fact]
    public async Task Create_SameNameAfterDelete_Succeeds()
    {
        var name = $"gone-{Guid.NewGuid():N}";
        var firstId = await TestDataFactory.CreateAgentAsync(_host, TestDataFactory.Agent(name));
        await _host.PublishAsync(new DeleteAgentCommand(firstId));

        var secondId = await TestDataFactory.CreateAgentAsync(_host, TestDataFactory.Agent(name));
        Assert.NotEqual(firstId, secondId);
    }

    [Fact]
    public async Task Update_ChangesAllFields_AndSyncsTools()
    {
        var (packageId, toolId, _, otherToolId) = await CreatePackagesWithToolsAsync();
        var dto = TestDataFactory.Agent(tools: [TestDataFactory.AgentTool(AgentToolType.Tool, toolId)]);
        var id = await TestDataFactory.CreateAgentAsync(_host, dto);
        // 详情里的工具条目不带 Id，被移除的行 Id 从库里取
        using var scope = _host.CreateScope();
        var beforeContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();
        var removedRowId = (await beforeContext.AgentTools.SingleAsync(x => x.AgentId == id)).Id;

        await TestDataFactory.UpdateAgentAsync(_host, id, new UpdateAgentDto
        {
            Name = dto.Name,
            Description = "新描述",
            Instructions = "新指令",
            Options = new AgentOptions { Temperature = 0.3f },
            Tools =
            [
                TestDataFactory.AgentTool(AgentToolType.ToolPackage, packageId),
                TestDataFactory.AgentTool(AgentToolType.Tool, otherToolId),
            ],
        });

        var agent = await TestDataFactory.GetAgentAsync(_host, id);
        Assert.Equal("新描述", agent!.Description);
        Assert.Equal("新指令", agent.Instructions);
        Assert.Equal(0.3f, agent.Options.Temperature);
        Assert.Equal(
            [(AgentToolType.ToolPackage, packageId), (AgentToolType.Tool, otherToolId)],
            agent.Tools.Select(x => (x.Type, x.TargetId)).ToList());

        // 没提交的条目在保存时软删（新开 scope 查库，避免拿到更新前已跟踪的实例）
        using var verifyScope = _host.CreateScope();
        var dbContext = verifyScope.ServiceProvider.GetRequiredService<CikeOSDbContext>();
        var removed = await dbContext.AgentTools.IgnoreQueryFilters().SingleAsync(x => x.Id == removedRowId);
        Assert.True(removed.IsDeleted);
        Assert.Equal(3, await dbContext.AgentTools.IgnoreQueryFilters().CountAsync(x => x.AgentId == id));
    }

    [Fact]
    public async Task Create_WithDuplicateToolBinding_Throws()
    {
        var (_, toolId, _, _) = await CreatePackagesWithToolsAsync();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => TestDataFactory.CreateAgentAsync(_host, tools:
        [
            TestDataFactory.AgentTool(AgentToolType.Tool, toolId),
            TestDataFactory.AgentTool(AgentToolType.Tool, toolId),
        ]));
        Assert.Contains("只能出现一次", ex.Message);
    }

    [Fact]
    public async Task Create_WithUnknownPackage_Throws()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            TestDataFactory.CreateAgentAsync(_host, tools: [TestDataFactory.AgentTool(AgentToolType.ToolPackage, 999999)]));
        Assert.Contains("工具包不存在", ex.Message);
    }

    [Fact]
    public async Task Create_WithUnknownTool_Throws()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            TestDataFactory.CreateAgentAsync(_host, tools: [TestDataFactory.AgentTool(AgentToolType.Tool, 999999)]));
        Assert.Contains("工具不存在", ex.Message);
    }

    [Fact]
    public async Task Create_ToolBelongingToWholePackage_Throws()
    {
        var (packageId, toolId, _, otherToolId) = await CreatePackagesWithToolsAsync();

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => TestDataFactory.CreateAgentAsync(_host, tools:
        [
            TestDataFactory.AgentTool(AgentToolType.ToolPackage, packageId),
            TestDataFactory.AgentTool(AgentToolType.Tool, toolId),
            TestDataFactory.AgentTool(AgentToolType.Tool, otherToolId),
        ]));
        Assert.Contains("整包绑定", ex.Message);

        // 别的包的工具与整包绑定可以并存
        var id = await TestDataFactory.CreateAgentAsync(_host, tools:
        [
            TestDataFactory.AgentTool(AgentToolType.ToolPackage, packageId),
            TestDataFactory.AgentTool(AgentToolType.Tool, otherToolId),
        ]);
        Assert.Equal(2, (await TestDataFactory.GetAgentAsync(_host, id))!.Tools.Count);
    }

    [Fact]
    public async Task Delete_RemovesAgent_AndSoftDeletesTools()
    {
        var (_, toolId, _, _) = await CreatePackagesWithToolsAsync();
        var id = await TestDataFactory.CreateAgentAsync(_host, tools: [TestDataFactory.AgentTool(AgentToolType.Tool, toolId)]);

        await _host.PublishAsync(new DeleteAgentCommand(id));

        Assert.Null(await TestDataFactory.GetAgentAsync(_host, id));
        using var scope = _host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();
        var rows = await dbContext.AgentTools.IgnoreQueryFilters().Where(x => x.AgentId == id).ToListAsync();
        var row = Assert.Single(rows);
        Assert.True(row.IsDeleted);
    }

    [Fact]
    public async Task Update_Or_Delete_Nonexistent_Throws()
    {
        var update = new UpdateAgentDto { Name = "无此", Instructions = "x", Options = new(), Tools = [] };
        var updateEx = await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(new UpdateAgentCommand(999999, update)));
        Assert.Contains("Agent 不存在", updateEx.Message);

        var deleteEx = await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(new DeleteAgentCommand(999999)));
        Assert.Contains("Agent 不存在", deleteEx.Message);
    }

    [Fact]
    public async Task OtherTenant_CannotSeeOrUpdate()
    {
        long id = 0;
        await WithTenantAsync(1, async () =>
        {
            id = await TestDataFactory.CreateAgentAsync(_host, TestDataFactory.Agent("租户一的 Agent"));
        });

        Assert.Null(await TestDataFactory.GetAgentAsync(_host, id));

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => TestDataFactory.UpdateAgentAsync(_host, id, new UpdateAgentDto
        {
            Name = "抢占",
            Instructions = "x",
            Options = new(),
            Tools = [],
        }));
        Assert.Contains("Agent 不存在", ex.Message);
    }

    [Fact]
    public async Task GetPagedList_Keyword_MatchesName()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var hitId = await TestDataFactory.CreateAgentAsync(_host, TestDataFactory.Agent($"hit-agent-{suffix}"));
        await TestDataFactory.CreateAgentAsync(_host, TestDataFactory.Agent($"other-agent-{suffix}", "描述里不含关键字"));

        var paged = PagedQuery($"hit-agent-{suffix}");
        await _host.PublishAsync(paged);
        Assert.Equal(hitId, Assert.Single(paged.Result!.Items).Id);
        Assert.Equal(1, paged.Result.Total);

        paged = PagedQuery($"nomatch-{suffix}");
        await _host.PublishAsync(paged);
        Assert.Empty(paged.Result!.Items);
    }

    [Fact]
    public async Task GetToolOptions_ReturnsTwoLevelPackagesWithTools_SortedByName()
    {
        var categoryId = await TestDataFactory.CreateCategoryAsync(_host, "选项分类");
        var packageA = await TestDataFactory.CreatePackageAsync(_host, categoryId, name: "A 包");
        var packageB = await TestDataFactory.CreatePackageAsync(_host, categoryId, name: "B 包");
        await TestDataFactory.CreatePackageAsync(_host, categoryId, name: "空包");
        var toolZ = await TestDataFactory.CreateToolAsync(_host, packageA, code: "zzz", name: "Z 工具");
        var toolA = await TestDataFactory.CreateToolAsync(_host, packageB, code: "aaa", name: "A 工具");

        var query = new GetAgentToolOptionsQuery();
        await _host.PublishAsync(query);

        // 共享宿主里还有其它用例造的工具包，只看自己这两个包
        var mine = query.Result!.Where(x => x.ToolPackageId == packageA || x.ToolPackageId == packageB).ToList();
        // 两层：顶层就是工具包，按名称排序；没有分类层
        Assert.Equal([(packageA, "A 包"), (packageB, "B 包")], mine.Select(x => (x.ToolPackageId, x.Name)).ToList());
        Assert.Equal([(toolZ, "Z 工具"), (toolA, "A 工具")], mine.SelectMany(x => x.Tools).Select(x => (x.ToolId, x.Name)).ToList());
        Assert.All(mine.SelectMany(x => x.Tools), x => Assert.Equal(ToolType.HttpMcp, x.Type));
        // TestDataFactory 造的工具没有参数
        Assert.All(mine.SelectMany(x => x.Tools), x => Assert.Equal(0, x.ParameterCount));
        // 没有工具的包不出现
        Assert.DoesNotContain(query.Result!, x => x.Name == "空包");
    }
}
