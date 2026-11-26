using Cike.MultiTenancy;
using Cike.Contracts.EntityDtos;
using Cike.OS.Application.Contracts.Models.Dtos;
using Cike.OS.Application.Models.Commands;
using Cike.OS.Application.Models.Queries;
using Cike.OS.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cike.OS.Tests;

public class ModelTests : IClassFixture<CikeOSTestHost>
{
    private readonly CikeOSTestHost _host;

    public ModelTests(CikeOSTestHost host)
    {
        _host = host;
    }

    /// <summary>切换环境租户（AsyncLocal 随异步链路流动，PublishAsync 内的租户过滤与插入填值都会跟随）。</summary>
    private async Task WithTenantAsync(long tenantId, Func<Task> action)
    {
        using var scope = _host.CreateScope();
        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        using var _ = currentTenant.Change(tenantId);
        await action();
    }

    private async Task<ModelDto> CreateModelAsync(string name, string displayName, string? description = "测试模型")
    {
        var id = await TestDataFactory.CreateModelAsync(_host, name, displayName, description);

        var query = new GetModelQuery(id);
        await _host.PublishAsync(query);
        return query.Result!;
    }

    [Fact]
    public async Task Create_Then_Get_ReturnsModel()
    {
        var model = await CreateModelAsync($"glm-{Guid.NewGuid():N}", "GLM 测试", "智谱模型条目");

        Assert.Equal("GLM 测试", model.DisplayName);
        Assert.Equal("智谱模型条目", model.Description);
        Assert.Empty(model.TagIds);
        Assert.True(model.Id > 0);
    }

    [Fact]
    public async Task Create_WithTags_ReturnsTagIds()
    {
        var tagId = await TestDataFactory.CreateTagAsync(_host, $"k-{Guid.NewGuid():N}", "标签");
        var model = await CreateModelAsync($"qwen-{Guid.NewGuid():N}", "Qwen 测试");

        var command = new UpdateModelCommand(model.Id, new UpdateModelDto
        {
            DisplayName = model.DisplayName,
            Description = model.Description,
            TagIds = [tagId, tagId],
        });
        await _host.PublishAsync(command);

        var query = new GetModelQuery(model.Id);
        await _host.PublishAsync(query);

        // 聚合不变量：重复标签引用被去重
        var tagId2 = Assert.Single(query.Result!.TagIds);
        Assert.Equal(tagId, tagId2);
    }

    [Fact]
    public async Task Create_WithoutDescription_StoresEmptyString()
    {
        var modelId = await TestDataFactory.CreateModelAsync(_host, $"nodesc-{Guid.NewGuid():N}", "无描述", null);

        var query = new GetModelQuery(modelId);
        await _host.PublishAsync(query);
        Assert.Equal(string.Empty, query.Result!.Description);
    }

    [Fact]
    public async Task Create_WithNonexistentTag_Throws()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => TestDataFactory.CreateModelAsync(_host, $"m-{Guid.NewGuid():N}", "缺标签", "d", [999999]));
        Assert.Contains("标签", ex.Message);
    }

    [Fact]
    public async Task Create_DuplicateName_Throws()
    {
        var name = $"dup-{Guid.NewGuid():N}";
        await TestDataFactory.CreateModelAsync(_host, name, "第一个");

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => TestDataFactory.CreateModelAsync(_host, name, "第二个"));
        Assert.Contains("模型名称", ex.Message);
    }

    [Fact]
    public async Task Create_DuplicateDisplayName_Throws()
    {
        var displayName = $"同名-{Guid.NewGuid():N}";
        await TestDataFactory.CreateModelAsync(_host, $"a-{Guid.NewGuid():N}", displayName);

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => TestDataFactory.CreateModelAsync(_host, $"b-{Guid.NewGuid():N}", displayName));
        Assert.Contains("显示名称", ex.Message);
    }

    [Fact]
    public async Task Update_DisplayNameConflict_Throws_OwnDisplayName_Succeeds_AndNameIsImmutable()
    {
        var first = await CreateModelAsync($"x-{Guid.NewGuid():N}", $"甲-{Guid.NewGuid():N}");
        var second = await CreateModelAsync($"y-{Guid.NewGuid():N}", $"乙-{Guid.NewGuid():N}");

        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(
            new UpdateModelCommand(second.Id, new UpdateModelDto
            {
                DisplayName = first.DisplayName,
                Description = second.Description,
                TagIds = [],
            })));

        await _host.PublishAsync(new UpdateModelCommand(second.Id, new UpdateModelDto
        {
            DisplayName = second.DisplayName,
            Description = "改后描述",
            TagIds = [],
        }));

        var query = new GetModelQuery(second.Id);
        await _host.PublishAsync(query);
        Assert.Equal("改后描述", query.Result!.Description);
        // 模型名称创建后不可变
        Assert.Equal(second.Name, query.Result.Name);
    }

    [Fact]
    public async Task Create_SameNameAfterSoftDelete_Succeeds()
    {
        var name = $"recycle-{Guid.NewGuid():N}";
        var firstId = await TestDataFactory.CreateModelAsync(_host, name, "先建");
        await _host.PublishAsync(new DeleteModelCommand(firstId));

        var secondId = await TestDataFactory.CreateModelAsync(_host, name, "重建");
        Assert.NotEqual(firstId, secondId);
    }

    [Fact]
    public async Task GetPagedList_Keyword_Filters()
    {
        var suffix = Guid.NewGuid().ToString("N");
        await TestDataFactory.CreateModelAsync(_host, $"kw-a-{suffix}", $"关键词甲{suffix}");
        await TestDataFactory.CreateModelAsync(_host, $"kw-b-{suffix}", $"其它{suffix}");

        var query = new GetModelPagedListQuery(new GetModelPagedListDto
        {
            Keyword = $"关键词甲{suffix}",
            Page = 1,
            PageSize = 10,
        });
        await _host.PublishAsync(query);

        var items = query.Result!.Items;
        Assert.All(items, x => Assert.Contains($"关键词甲{suffix}", x.DisplayName));
        Assert.Contains(items, x => x.Name == $"kw-a-{suffix}");
    }

    [Fact]
    public async Task Delete_RemovesModel()
    {
        var model = await CreateModelAsync($"del-{Guid.NewGuid():N}", "待删除");

        await _host.PublishAsync(new DeleteModelCommand(model.Id));

        var query = new GetModelQuery(model.Id);
        await _host.PublishAsync(query);
        Assert.Null(query.Result);
    }

    [Fact]
    public async Task Delete_ModelWithMapping_Throws()
    {
        var modelId = await TestDataFactory.CreateModelAsync(_host, $"guard-{Guid.NewGuid():N}", "映射守护");
        await TestDataFactory.CreateProviderWithMappingsAsync(_host, $"guard-{Guid.NewGuid():N}", [TestDataFactory.Mapping(modelId)]);

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _host.PublishAsync(new DeleteModelCommand(modelId)));
        Assert.Contains("映射", ex.Message);
    }

    [Fact]
    public async Task Delete_ModelWithMapping_FromAnotherTenant_Throws()
    {
        // 平台目录在默认租户（0）下创建
        var modelId = await TestDataFactory.CreateModelAsync(_host, $"guard-{Guid.NewGuid():N}", "跨租户守护");

        // 租户 1 的提供方映射到该模型上
        await WithTenantAsync(1, () => TestDataFactory.CreateProviderWithMappingsAsync(
            _host, "租户一提供商", [TestDataFactory.Mapping(modelId)]));

        // 回到租户 0 删除模型：全库映射计数应挡住删除
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _host.PublishAsync(new DeleteModelCommand(modelId)));
        Assert.Contains("映射", ex.Message);
    }

    [Fact]
    public async Task Catalog_IsSharedAcrossTenants_DuplicateNameRejectedEverywhere()
    {
        var name = $"shared-{Guid.NewGuid():N}";
        var modelId = await TestDataFactory.CreateModelAsync(_host, name, "平台目录");

        await WithTenantAsync(1, async () =>
        {
            // 目录对所有租户可见
            var query = new GetModelQuery(modelId);
            await _host.PublishAsync(query);
            Assert.NotNull(query.Result);

            // 全局唯一：租户 1 也不能重复创建同名模型
            await Assert.ThrowsAsync<UserFriendlyException>(
                () => TestDataFactory.CreateModelAsync(_host, name, "租户一重复"));
        });
    }
}
