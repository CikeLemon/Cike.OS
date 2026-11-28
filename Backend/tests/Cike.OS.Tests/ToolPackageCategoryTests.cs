using Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;
using Cike.OS.Application.ToolPackageCategories.Commands;
using Cike.OS.Application.ToolPackageCategories.Queries;
using Cike.OS.Tests.Infrastructure;
using Xunit;

namespace Cike.OS.Tests;

public class ToolPackageCategoryTests : IClassFixture<CikeOSTestHost>
{
    private readonly CikeOSTestHost _host;

    public ToolPackageCategoryTests(CikeOSTestHost host)
    {
        _host = host;
    }

    private async Task<ToolPackageCategoryDetailDto> CreateCategoryAsync(string name)
    {
        var id = await TestDataFactory.CreateCategoryAsync(_host, name);

        var query = new GetToolPackageCategoryDetailQuery(id);
        await _host.PublishAsync(query);
        return query.Result!;
    }

    [Fact]
    public async Task Create_Then_Get_ReturnsCategory()
    {
        var category = await CreateCategoryAsync("生产力");

        Assert.Equal("生产力", category.Name);
        Assert.True(category.Id > 0);
    }

    [Fact]
    public async Task Create_TrimsName()
    {
        var category = await CreateCategoryAsync("  数据分析  ");

        Assert.Equal("数据分析", category.Name);
    }

    [Fact]
    public async Task List_OrdersByName()
    {
        await CreateCategoryAsync($"b-{Guid.NewGuid():N}");
        await CreateCategoryAsync($"a-{Guid.NewGuid():N}");

        var query = new GetToolPackageCategoryListQuery();
        await _host.PublishAsync(query);

        var names = query.Result!.Select(x => x.Name).ToList();
        Assert.True(names.Count >= 2);
        Assert.Equal(names.OrderBy(x => x, StringComparer.Ordinal), names);
    }

    [Fact]
    public async Task Update_RenamesCategory()
    {
        var category = await CreateCategoryAsync("旧名");

        await _host.PublishAsync(new UpdateToolPackageCategoryCommand(category.Id, new UpdateToolPackageCategoryDto {
            Name = "新名",
        }));

        var query = new GetToolPackageCategoryDetailQuery(category.Id);
        await _host.PublishAsync(query);

        Assert.Equal("新名", query.Result!.Name);
    }

    [Fact]
    public async Task Delete_RemovesCategory()
    {
        var category = await CreateCategoryAsync("待删除");

        await _host.PublishAsync(new DeleteToolPackageCategoryCommand(category.Id));

        var query = new GetToolPackageCategoryDetailQuery(category.Id);
        await _host.PublishAsync(query);

        Assert.Null(query.Result);
    }

    [Fact]
    public async Task Delete_WithPackages_Throws()
    {
        var categoryId = await TestDataFactory.CreateCategoryAsync(_host, "有包的分类");
        await TestDataFactory.CreatePackageAsync(_host, categoryId);

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => _host.PublishAsync(new DeleteToolPackageCategoryCommand(categoryId)));
    }

    [Fact]
    public async Task Create_DuplicateName_AfterTrim_Throws()
    {
        var categoryId = await TestDataFactory.CreateCategoryAsync(_host, "数据");

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => TestDataFactory.CreateCategoryAsync(_host, "  数据 "));
        Assert.Contains("已存在", ex.Message);

        var query = new GetToolPackageCategoryListQuery();
        await _host.PublishAsync(query);
        var survivors = query.Result!.Where(x => x.Name == "数据").ToList();
        var survivor = Assert.Single(survivors);
        Assert.Equal(categoryId, survivor.Id);
    }

    [Fact]
    public async Task Update_RenameToExistingName_Throws_RenameToOwnName_Succeeds()
    {
        var categoryId = await TestDataFactory.CreateCategoryAsync(_host, "甲");
        var otherId = await TestDataFactory.CreateCategoryAsync(_host, "乙");

        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(
            new UpdateToolPackageCategoryCommand(otherId, new UpdateToolPackageCategoryDto { Name = "甲" })));

        await _host.PublishAsync(new UpdateToolPackageCategoryCommand(categoryId, new UpdateToolPackageCategoryDto { Name = " 甲 " }));

        var query = new GetToolPackageCategoryDetailQuery(categoryId);
        await _host.PublishAsync(query);
        Assert.Equal("甲", query.Result!.Name);
    }

    [Fact]
    public async Task Create_SameNameAfterSoftDelete_Succeeds()
    {
        var firstId = await TestDataFactory.CreateCategoryAsync(_host, "可回收");
        await _host.PublishAsync(new DeleteToolPackageCategoryCommand(firstId));

        var secondId = await TestDataFactory.CreateCategoryAsync(_host, "可回收");
        Assert.NotEqual(firstId, secondId);
    }
}
