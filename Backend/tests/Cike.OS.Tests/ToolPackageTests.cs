using Cike.OS.Application.Contracts.ToolPackages.Dtos;
using Cike.OS.Application.ToolPackages.Commands;
using Cike.OS.Application.ToolPackages.Queries;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Tests.Infrastructure;
using Xunit;

namespace Cike.OS.Tests;

public class ToolPackageTests : IClassFixture<CikeOSTestHost>
{
    private readonly CikeOSTestHost _host;

    public ToolPackageTests(CikeOSTestHost host)
    {
        _host = host;
    }

    private async Task<long> CreateCategoryAsync()
    {
        return await TestDataFactory.CreateCategoryAsync(_host, $"分类-{Guid.NewGuid():N}");
    }

    [Fact]
    public async Task Create_Then_Get_ReturnsPackageWithCategoryName()
    {
        var categoryId = await CreateCategoryAsync();
        var packageId = await TestDataFactory.CreatePackageAsync(_host, categoryId, name: "搜索工具包");

        var query = new GetToolPackageDetailQuery(packageId);
        await _host.PublishAsync(query);

        Assert.Equal("搜索工具包", query.Result!.Name);
        Assert.Equal(ToolPackageStatus.Draft, query.Result.Status);
        Assert.StartsWith("分类-", query.Result.ToolPackageCategoryName);
    }

    [Fact]
    public async Task Create_WithUnknownCategory_Throws()
    {
        await Assert.ThrowsAsync<UserFriendlyException>(
            () => TestDataFactory.CreatePackageAsync(_host, categoryId: 123456789));
    }

    [Fact]
    public async Task Create_WithDuplicateCode_Throws()
    {
        var categoryId = await CreateCategoryAsync();
        var code = $"dup-{Guid.NewGuid():N}";
        await TestDataFactory.CreatePackageAsync(_host, categoryId, code);

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => TestDataFactory.CreatePackageAsync(_host, categoryId, code));
    }

    [Fact]
    public async Task Create_SameCodeAfterSoftDelete_Succeeds()
    {
        var categoryId = await CreateCategoryAsync();
        var code = $"recycle-{Guid.NewGuid():N}";
        var packageId = await TestDataFactory.CreatePackageAsync(_host, categoryId, code);

        await _host.PublishAsync(new DeleteToolPackageCommand(packageId));

        var recreated = await TestDataFactory.CreatePackageAsync(_host, categoryId, code);
        Assert.NotEqual(packageId, recreated);
    }

    [Fact]
    public async Task Update_ChangesFields_AndRejectsDuplicateCode()
    {
        var categoryId = await CreateCategoryAsync();
        var otherCategoryId = await TestDataFactory.CreateCategoryAsync(_host, "其他分类");
        var packageId = await TestDataFactory.CreatePackageAsync(_host, categoryId, code: "update-me");
        await TestDataFactory.CreatePackageAsync(_host, categoryId, code: "taken");

        await _host.PublishAsync(new UpdateToolPackageCommand(packageId, new UpdateToolPackageDto {
            Code = "update-me",
            Name = "改名后",
            Description = "描述",
            Icon = "https://example.com/icon.svg",
            Readme = "# readme",
            ToolPackageCategoryId = otherCategoryId,
            Status = ToolPackageStatus.Published,
        }));

        var query = new GetToolPackageDetailQuery(packageId);
        await _host.PublishAsync(query);

        Assert.Equal("改名后", query.Result!.Name);
        Assert.Equal("描述", query.Result.Description);
        Assert.Equal("# readme", query.Result.Readme);
        Assert.Equal(otherCategoryId, query.Result.ToolPackageCategoryId);
        Assert.Equal(ToolPackageStatus.Published, query.Result.Status);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(new UpdateToolPackageCommand(packageId, new UpdateToolPackageDto {
            Code = "taken",
            Name = "改名后",
            ToolPackageCategoryId = categoryId,
            Status = ToolPackageStatus.Draft,
        })));
    }

    [Fact]
    public async Task GetPagedList_FiltersByKeywordCategoryAndStatus()
    {
        var categoryId = await CreateCategoryAsync();
        var marker = Guid.NewGuid().ToString("N")[..8];
        await TestDataFactory.CreatePackageAsync(_host, categoryId, name: $"搜索-{marker}-甲", status: ToolPackageStatus.Published);
        await TestDataFactory.CreatePackageAsync(_host, categoryId, name: $"搜索-{marker}-乙", status: ToolPackageStatus.Offline);
        await TestDataFactory.CreatePackageAsync(_host, categoryId, name: $"无关-{marker}", status: ToolPackageStatus.Published);

        var query = new GetToolPackagePagedListQuery(new GetToolPackagePagedListRequestDto
        {
            Keyword = marker,
            ToolPackageCategoryId = categoryId,
            Status = ToolPackageStatus.Published,
            Page = 1,
            PageSize = 10,
            Sorting = "Id desc", // 前端传入的 Dynamic LINQ 排序，后端透传
        });
        await _host.PublishAsync(query);

        Assert.Equal(2, query.Result!.Total);
        Assert.Equal([$"无关-{marker}", $"搜索-{marker}-甲"], query.Result.Items.Select(x => x.Name).ToList());
        Assert.DoesNotContain(query.Result.Items, x => x.Name == $"搜索-{marker}-乙");
        Assert.All(query.Result.Items, x => Assert.Equal(ToolPackageStatus.Published, x.Status));
        Assert.All(query.Result.Items, x => Assert.StartsWith("分类-", x.ToolPackageCategoryName));
    }

    [Fact]
    public async Task GetPagedList_Paginates()
    {
        var categoryId = await CreateCategoryAsync();
        var marker = Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 3; i++)
        {
            await TestDataFactory.CreatePackageAsync(_host, categoryId, name: $"分页-{marker}-{i}");
        }

        var query = new GetToolPackagePagedListQuery(new GetToolPackagePagedListRequestDto
        {
            Keyword = marker,
            Page = 2,
            PageSize = 2,
        });
        await _host.PublishAsync(query);

        Assert.Equal(3, query.Result!.Total);
        Assert.Single(query.Result.Items);
    }

    [Fact]
    public async Task Delete_WithTools_Throws()
    {
        var categoryId = await CreateCategoryAsync();
        var packageId = await TestDataFactory.CreatePackageAsync(_host, categoryId);
        await TestDataFactory.CreateToolAsync(_host, packageId);

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => _host.PublishAsync(new DeleteToolPackageCommand(packageId)));
    }

    [Fact]
    public async Task Delete_WithoutTools_SoftDeletes()
    {
        var categoryId = await CreateCategoryAsync();
        var packageId = await TestDataFactory.CreatePackageAsync(_host, categoryId);

        await _host.PublishAsync(new DeleteToolPackageCommand(packageId));

        var query = new GetToolPackageDetailQuery(packageId);
        await _host.PublishAsync(query);
        Assert.Null(query.Result);
    }
}
