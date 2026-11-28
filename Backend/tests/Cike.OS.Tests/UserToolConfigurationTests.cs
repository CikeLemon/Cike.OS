using Cike.Data.Domain.Repositories;
using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;
using Cike.OS.Application.Tools.Commands;
using Cike.OS.Application.UserToolConfigurations.Commands;
using Cike.OS.Application.UserToolConfigurations.Queries;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cike.OS.Tests;

public class UserToolConfigurationTests : IClassFixture<CikeOSTestHost>
{
    private readonly CikeOSTestHost _host;

    public UserToolConfigurationTests(CikeOSTestHost host)
    {
        _host = host;

        using var scope = _host.CreateScope();
        var currentUser = scope.ServiceProvider.GetRequiredService<FakeCurrentUser>();
        currentUser.Id = "user-1";
    }

    /// <summary>带一个参数的工具，挂在一个真实的分类 / 工具包下（分页列表要联合工具包与分类）。</summary>
    private async Task<long> CreateToolAsync(bool required = true)
    {
        var categoryId = await TestDataFactory.CreateCategoryAsync(_host, $"分类-{Guid.NewGuid():N}");
        var packageId = await TestDataFactory.CreatePackageAsync(_host, categoryId, name: $"包-{Guid.NewGuid():N}");
        return await TestDataFactory.CreateToolAsync(_host, packageId, name: "Test tool", parameters:
        [
            new ToolParameterDefinition
            {
                Code = "ak",
                Name = "Access key",
                Required = required,
                Sensitive = true,
            },
        ]);
    }

    [Fact]
    public async Task Save_Then_Get_ReturnsUserValues()
    {
        var tool = await CreateToolAsync();

        var saveCommand = new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = tool,
            Values = new Dictionary<string, string> { ["ak"] = "user-123" },
        });
        await _host.PublishAsync(saveCommand);

        var query = new GetUserToolConfigurationDetailQuery(tool);
        await _host.PublishAsync(query);

        Assert.NotNull(query.Result);
        Assert.Equal(tool, query.Result!.ToolId);
        Assert.Equal("user-123", query.Result.Values["ak"]);
    }

    /// <summary>编辑弹窗靠详情里的工具名与参数定义生成字段：两者必须跟着已存的值一起返回。</summary>
    [Fact]
    public async Task GetDetail_ReturnsToolNameAndCurrentParameterDefinitions()
    {
        var tool = await CreateToolWithPackageAsync($"详情-{Guid.NewGuid():N}");
        await _host.PublishAsync(new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = tool,
            Values = new Dictionary<string, string> { ["ak"] = "v" },
        }));

        var query = new GetUserToolConfigurationDetailQuery(tool);
        await _host.PublishAsync(query);

        var detail = query.Result!;
        Assert.Equal(tool, detail.ToolId);
        Assert.Equal("ak", Assert.Single(detail.Parameters).Code);
        Assert.Equal("v", detail.Values["ak"]);
        Assert.False(string.IsNullOrEmpty(detail.ToolName));
    }

    [Fact]
    public async Task GetDetail_OtherUsersConfigurationOrDeletedTool_ReturnsNull()
    {
        var tool = await CreateToolAsync();

        // 别的用户配了不算当前用户的
        SetUser("someone-else");
        await _host.PublishAsync(new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = tool,
            Values = new Dictionary<string, string> { ["ak"] = "theirs" },
        }));
        SetUser("user-1");

        var query = new GetUserToolConfigurationDetailQuery(tool);
        await _host.PublishAsync(query);
        Assert.Null(query.Result);

        // 当前用户也配一份，然后把工具删掉：与卡片列表的内连接口径一致，视为不存在
        await _host.PublishAsync(new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = tool,
            Values = new Dictionary<string, string> { ["ak"] = "mine" },
        }));
        await _host.PublishAsync(new DeleteToolCommand(tool));
        var deleted = new GetUserToolConfigurationDetailQuery(tool);
        await _host.PublishAsync(deleted);
        Assert.Null(deleted.Result);
    }

    [Fact]
    public async Task Save_Twice_UpdatesExistingConfiguration()
    {
        var tool = await CreateToolAsync();

        var first = new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = tool,
            Values = new Dictionary<string, string> { ["ak"] = "user-123" },
        });
        await _host.PublishAsync(first);

        var second = new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = tool,
            Values = new Dictionary<string, string> { ["ak"] = "user-456" },
        });
        await _host.PublishAsync(second);

        var query = new GetUserToolConfigurationDetailQuery(tool);
        await _host.PublishAsync(query);

        Assert.Equal("user-456", query.Result!.Values["ak"]);
    }

    [Fact]
    public async Task Save_MissingRequiredParameter_Throws()
    {
        var tool = await CreateToolAsync(required: true);

        var command = new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = tool,
            Values = new Dictionary<string, string>(),
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(command));
    }

    [Fact]
    public async Task Delete_RemovesConfiguration()
    {
        var tool = await CreateToolAsync();

        var saveCommand = new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = tool,
            Values = new Dictionary<string, string> { ["ak"] = "user-123" },
        });
        await _host.PublishAsync(saveCommand);

        await _host.PublishAsync(new DeleteUserToolConfigurationCommand(tool));

        var query = new GetUserToolConfigurationDetailQuery(tool);
        await _host.PublishAsync(query);

        Assert.Null(query.Result);
    }

    /// <summary>一个分类下带参数的工具，供分页列表（联合工具 / 包 / 分类）用。</summary>
    private async Task<long> CreateToolWithPackageAsync(string? name = null)
    {
        var categoryId = await TestDataFactory.CreateCategoryAsync(_host, $"分类-{Guid.NewGuid():N}");
        var packageId = await TestDataFactory.CreatePackageAsync(_host, categoryId, name: $"包-{Guid.NewGuid():N}");
        return await TestDataFactory.CreateToolWithParametersAsync(_host, packageId, name);
    }

    private void SetUser(string? userId)
    {
        using var scope = _host.CreateScope();
        scope.ServiceProvider.GetRequiredService<FakeCurrentUser>().Id = userId;
    }

    [Fact]
    public async Task SaveBatch_InsertsNew_AndUpdatesExisting()
    {
        var newTool = await CreateToolWithPackageAsync();
        var existingTool = await CreateToolWithPackageAsync();
        await _host.PublishAsync(new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = existingTool,
            Values = new Dictionary<string, string> { ["ak"] = "old" },
        }));

        await _host.PublishAsync(new SaveUserToolConfigurationsCommand(
        [
            new SaveUserToolConfigurationDto { ToolId = newTool, Values = new Dictionary<string, string> { ["ak"] = "new" } },
            new SaveUserToolConfigurationDto { ToolId = existingTool, Values = new Dictionary<string, string> { ["ak"] = "updated" } },
        ]));

        var first = new GetUserToolConfigurationDetailQuery(newTool);
        await _host.PublishAsync(first);
        Assert.Equal("new", first.Result!.Values["ak"]);

        var second = new GetUserToolConfigurationDetailQuery(existingTool);
        await _host.PublishAsync(second);
        Assert.Equal("updated", second.Result!.Values["ak"]);

        // 同一工具仍然只有一条配置
        var paged = new GetUserToolConfigurationPagedListQuery(null, new Cike.Contracts.EntityDtos.PagedAndSortedResultRequest { Page = 1, PageSize = 100 });
        await _host.PublishAsync(paged);
        Assert.Equal(1, paged.Result!.Items.Count(x => x.ToolId == existingTool));
    }

    [Fact]
    public async Task SaveBatch_MissingRequired_Throws_AndSavesNothing()
    {
        var goodTool = await CreateToolWithPackageAsync();
        var badTool = await CreateToolWithPackageAsync();

        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(new SaveUserToolConfigurationsCommand(
        [
            new SaveUserToolConfigurationDto { ToolId = goodTool, Values = new Dictionary<string, string> { ["ak"] = "ok" } },
            new SaveUserToolConfigurationDto { ToolId = badTool, Values = new Dictionary<string, string>() },
        ])));

        var query = new GetUserToolConfigurationDetailQuery(goodTool);
        await _host.PublishAsync(query);
        Assert.Null(query.Result);
    }

    [Fact]
    public async Task GetPagedList_ReturnsCards_OnlyForCurrentUser_FilteredByKeyword()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var mine = await CreateToolWithPackageAsync($"天气查询-{suffix}");
        var other = await CreateToolWithPackageAsync($"别人的-{suffix}");

        SetUser("user-1");
        await _host.PublishAsync(new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = mine,
            Values = new Dictionary<string, string> { ["ak"] = "mine" },
        }));

        SetUser("user-2");
        await _host.PublishAsync(new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = other,
            Values = new Dictionary<string, string> { ["ak"] = "theirs" },
        }));

        SetUser("user-1");
        var paged = new GetUserToolConfigurationPagedListQuery($"天气查询-{suffix}", new Cike.Contracts.EntityDtos.PagedAndSortedResultRequest { Page = 1, PageSize = 10, Sorting = "UpdatedAt desc" });
        await _host.PublishAsync(paged);
        var card = Assert.Single(paged.Result!.Items);
        Assert.Equal(mine, card.ToolId);
        Assert.Equal($"天气查询-{suffix}", card.ToolName);
        Assert.Equal(ToolType.HttpMcp, card.ToolType);
        Assert.StartsWith("包-", card.ToolPackageName);
        Assert.StartsWith("分类-", card.ToolPackageCategoryName);
        Assert.Equal("ak", Assert.Single(card.Parameters).Code);
        Assert.Equal("mine", card.Values["ak"]);

        // 不带关键字也看不到 user-2 的配置
        paged = new GetUserToolConfigurationPagedListQuery(null, new Cike.Contracts.EntityDtos.PagedAndSortedResultRequest { Page = 1, PageSize = 100 });
        await _host.PublishAsync(paged);
        Assert.Contains(paged.Result!.Items, x => x.ToolId == mine);
        Assert.DoesNotContain(paged.Result!.Items, x => x.ToolId == other);

        SetUser("user-1");
    }

    [Fact]
    public async Task GetToolOptions_MarksConfiguredOnlyForCurrentUser()
    {
        var configured = await CreateToolWithPackageAsync();
        var notConfigured = await CreateToolWithPackageAsync();

        SetUser("user-9");
        await _host.PublishAsync(new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = configured,
            Values = new Dictionary<string, string> { ["ak"] = "v" },
        }));

        var query = new GetUserToolConfigurationToolOptionsQuery();
        await _host.PublishAsync(query);

        Assert.True(query.Result!.Single(x => x.ToolId == configured).Configured);
        var pending = query.Result!.Single(x => x.ToolId == notConfigured);
        Assert.False(pending.Configured);
        Assert.StartsWith("包-", pending.ToolPackageName);
        // 全部候选都带参数定义
        Assert.All(query.Result!, x => Assert.NotEmpty(x.Parameters));

        SetUser("user-1");
    }
}
