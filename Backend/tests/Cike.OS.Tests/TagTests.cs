using Cike.MultiTenancy;
using Cike.Contracts.EntityDtos;
using Cike.OS.Application.Contracts.Tags.Dtos;
using Cike.OS.Application.ModelProviders.Commands;
using Cike.OS.Application.ModelProviders.Queries;
using Cike.OS.Application.Tags.Commands;
using Cike.OS.Application.Tags.Queries;
using Cike.OS.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cike.OS.Tests;

public class TagTests : IClassFixture<CikeOSTestHost>
{
    private readonly CikeOSTestHost _host;

    public TagTests(CikeOSTestHost host)
    {
        _host = host;
    }

    private static string NewKey() => $"k-{Guid.NewGuid():N}";

    /// <summary>切换环境租户（AsyncLocal 随异步链路流动，PublishAsync 内的租户过滤都会跟随）。</summary>
    private async Task WithTenantAsync(long tenantId, Func<Task> action)
    {
        using var scope = _host.CreateScope();
        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        using var _ = currentTenant.Change(tenantId);
        await action();
    }

    private async Task<long> CreateTagAsync(string key, string name = "测试标签")
    {
        var command = new CreateTagCommand(new CreateTagDto { Key = key, Name = name });
        await _host.PublishAsync(command);
        return command.Id;
    }

    /// <summary>按关键字查第一页（每页 10 条）。</summary>
    private static GetTagPagedListQuery PagedQuery(string? keyword) =>
        new(keyword, new PagedAndSortedResultRequest { Page = 1, PageSize = 10 });

    /// <summary>按键查分页列表：键在未删除的标签里唯一，命中即单条。</summary>
    private async Task<TagDto> GetByKeyAsync(string key)
    {
        var query = PagedQuery(key);
        await _host.PublishAsync(query);
        return Assert.Single(query.Result!.Items);
    }

    private async Task<List<TagDto>> GetListAsync()
    {
        var query = new GetTagListQuery();
        await _host.PublishAsync(query);
        return query.Result!;
    }

    [Fact]
    public async Task Create_ThenLists_ReturnsTag()
    {
        var key = NewKey();

        var id = await CreateTagAsync(key, "视觉");

        var paged = await GetByKeyAsync(key);
        Assert.Equal(id, paged.Id);
        Assert.Equal(key, paged.Key);
        Assert.Equal("视觉", paged.Name);
        Assert.Contains(await GetListAsync(), x => x.Id == id);
    }

    [Fact]
    public async Task Create_DuplicateKey_Throws()
    {
        var key = NewKey();
        await CreateTagAsync(key);

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateTagAsync(key, "重名"));
        Assert.Contains("已存在", ex.Message);
    }

    [Fact]
    public async Task Create_SameKeyAfterSoftDelete_Succeeds()
    {
        var key = NewKey();
        var firstId = await CreateTagAsync(key, "先建");
        await _host.PublishAsync(new DeleteTagCommand(firstId));

        var secondId = await CreateTagAsync(key, "重建");
        Assert.NotEqual(firstId, secondId);
        Assert.Equal("重建", (await GetByKeyAsync(key)).Name);
    }

    [Fact]
    public async Task Update_ChangesName_KeyUnchanged()
    {
        var key = NewKey();
        var id = await CreateTagAsync(key, "旧名");

        await _host.PublishAsync(new UpdateTagCommand(id, new UpdateTagDto { Name = "新名" }));

        var tag = await GetByKeyAsync(key);
        Assert.Equal("新名", tag.Name);
        Assert.Equal(key, tag.Key);
    }

    [Fact]
    public async Task Update_Nonexistent_Throws()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _host.PublishAsync(new UpdateTagCommand(999999, new UpdateTagDto { Name = "无此标签" })));
        Assert.Contains("标签不存在", ex.Message);
    }

    [Fact]
    public async Task Delete_RemovesFromBothLists()
    {
        var key = NewKey();
        var id = await CreateTagAsync(key, "待删除");

        await _host.PublishAsync(new DeleteTagCommand(id));

        var paged = PagedQuery(key);
        await _host.PublishAsync(paged);
        Assert.Empty(paged.Result!.Items);
        Assert.DoesNotContain(await GetListAsync(), x => x.Id == id);
    }

    [Fact]
    public async Task Delete_Nonexistent_Throws()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => _host.PublishAsync(new DeleteTagCommand(999999)));
        Assert.Contains("标签不存在", ex.Message);
    }

    [Fact]
    public async Task Delete_ReferencedByModel_Succeeds_AndModelUntouched()
    {
        var tagId = await CreateTagAsync(NewKey(), "被引用");
        var command = new CreateModelProviderCommand(TestDataFactory.Provider(
            $"ref-{Guid.NewGuid():N}", models: [TestDataFactory.Model("glm-5.3", tagIds: [tagId])]));
        await _host.PublishAsync(command);

        // 没有引用守护：直接删成功
        await _host.PublishAsync(new DeleteTagCommand(tagId));

        var query = new GetModelProviderQuery(command.Id);
        await _host.PublishAsync(query);
        // 模型数据不被动，引用仍留在聚合上（界面回填时才被过滤）
        Assert.Equal([tagId], Assert.Single(query.Result!.Models).TagIds);
    }

    [Fact]
    public async Task GetPagedList_Keyword_MatchesKeyOrName()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var byKey = await CreateTagAsync($"key-{suffix}", "按键命中");
        var byName = await CreateTagAsync($"other-{suffix}", $"按名命中-{suffix}");

        var paged = PagedQuery(suffix);
        await _host.PublishAsync(paged);
        Assert.Equal(2, paged.Result!.Total);
        Assert.Contains(paged.Result.Items, x => x.Id == byKey);
        Assert.Contains(paged.Result.Items, x => x.Id == byName);

        paged = PagedQuery($"key-{suffix}");
        await _host.PublishAsync(paged);
        Assert.Equal(byKey, Assert.Single(paged.Result!.Items).Id);

        paged = PagedQuery($"按名命中-{suffix}");
        await _host.PublishAsync(paged);
        Assert.Equal(byName, Assert.Single(paged.Result!.Items).Id);

        paged = PagedQuery($"nomatch-{suffix}");
        await _host.PublishAsync(paged);
        Assert.Empty(paged.Result!.Items);
        Assert.Equal(0, paged.Result.Total);
    }

    [Fact]
    public async Task Catalog_IsSharedAcrossTenants_DuplicateKeyRejectedEverywhere()
    {
        var key = NewKey();
        var id = await CreateTagAsync(key, "平台目录");

        await WithTenantAsync(1, async () =>
        {
            // 目录对所有租户可见
            Assert.Contains(await GetListAsync(), x => x.Id == id);

            // 全局唯一：租户 1 也不能重复创建同键标签
            await Assert.ThrowsAsync<UserFriendlyException>(() => CreateTagAsync(key, "租户一重复"));
        });
    }
}
