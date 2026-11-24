using Cike.OS.Application.Contracts.Tools.Dtos;
using Cike.OS.Application.SkillPackages;
using Cike.OS.Application.SkillPackages.Commands;
using Cike.OS.Application.Tools.Commands;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cike.OS.Tests;

public class SkillPackageTests : IClassFixture<CikeOSTestHost>
{
    private readonly CikeOSTestHost _host;

    public SkillPackageTests(CikeOSTestHost host)
    {
        _host = host;
    }

    private static MemoryStream StreamOf(byte[] bytes) => new(bytes);

    /// <summary>相对路径 skills/{tenant}/{guid} → 物理路径 {RootPath}/{tenant}/{guid}</summary>
    private string PhysicalOf(string directory) => Path.Combine(_host.SkillRootPath, directory["skills/".Length..]);

    private int CountDirectories()
    {
        return Directory.Exists(_host.SkillRootPath)
            ? Directory.GetDirectories(_host.SkillRootPath, "*", SearchOption.AllDirectories).Length
            : 0;
    }

    [Fact]
    public async Task Upload_ValidZip_ReturnsRelativeDirectory_AndExtractsFiles()
    {
        var command = new UploadSkillPackageCommand(StreamOf(ZipHelper.CreateSkillPackage()));
        await _host.PublishAsync(command);

        Assert.Matches(@"^skills/0/[0-9a-f]{32}$", command.Directory);

        var physical = PhysicalOf(command.Directory!);
        Assert.True(File.Exists(Path.Combine(physical, "SKILL.md")));
        Assert.True(File.Exists(Path.Combine(physical, "scripts", "run.py")));
    }

    [Fact]
    public async Task Upload_MissingSkillMd_Throws_AndLeavesNoDirectory()
    {
        var before = CountDirectories();

        var command = new UploadSkillPackageCommand(StreamOf(ZipHelper.Create(("README.md", "no skill here"))));
        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(command));

        Assert.Equal(before, CountDirectories());
    }

    [Fact]
    public async Task Upload_NestedSkillMdOnly_Throws()
    {
        var command = new UploadSkillPackageCommand(StreamOf(ZipHelper.Create(("wrapped/SKILL.md", "# nested"))));
        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(command));
    }

    [Fact]
    public async Task Upload_EmptyZip_Throws()
    {
        var command = new UploadSkillPackageCommand(StreamOf(ZipHelper.Create()));
        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(command));
    }

    [Fact]
    public async Task Upload_InvalidZip_Throws()
    {
        var command = new UploadSkillPackageCommand(StreamOf([1, 2, 3, 4, 5, 6, 7, 8]));
        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(command));
    }

    [Fact]
    public async Task Upload_ZipSlipEntry_Throws_AndWritesNothingOutside()
    {
        var before = CountDirectories();
        var bytes = ZipHelper.Create(
            ("SKILL.md", "# skill"),
            ("../evil.txt", "pwned"));

        var command = new UploadSkillPackageCommand(StreamOf(bytes));
        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(command));

        Assert.Equal(before, CountDirectories());
        Assert.False(File.Exists(Path.Combine(_host.SkillRootPath, "evil.txt")));
    }

    [Fact]
    public async Task Upload_OversizedZip_Throws()
    {
        var big = ZipHelper.Create(("SKILL.md", ZipHelper.RandomText(4096)));
        Assert.True(big.Length > 1024);

        var command = new UploadSkillPackageCommand(StreamOf(big));
        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(command));
    }

    [Fact]
    public async Task DeleteSkillTool_RemovesDirectory()
    {
        var categoryId = await TestDataFactory.CreateCategoryAsync(_host, $"技能分类-{Guid.NewGuid():N}");
        var packageId = await TestDataFactory.CreatePackageAsync(_host, categoryId);

        var upload = new UploadSkillPackageCommand(StreamOf(ZipHelper.CreateSkillPackage()));
        await _host.PublishAsync(upload);

        var toolId = await TestDataFactory.CreateToolAsync(_host, packageId, type: ToolType.Skill, skillDirectory: upload.Directory!);
        var physical = PhysicalOf(upload.Directory!);
        Assert.True(Directory.Exists(physical));

        await _host.PublishAsync(new DeleteToolCommand(toolId));

        Assert.False(Directory.Exists(physical));
    }

    [Fact]
    public async Task UpdateSkillTool_WithNewDirectory_RemovesOldDirectory()
    {
        var categoryId = await TestDataFactory.CreateCategoryAsync(_host, $"技能分类-{Guid.NewGuid():N}");
        var packageId = await TestDataFactory.CreatePackageAsync(_host, categoryId);

        var first = new UploadSkillPackageCommand(StreamOf(ZipHelper.CreateSkillPackage("v1")));
        await _host.PublishAsync(first);
        var second = new UploadSkillPackageCommand(StreamOf(ZipHelper.CreateSkillPackage("v2")));
        await _host.PublishAsync(second);

        var toolId = await TestDataFactory.CreateToolAsync(_host, packageId, type: ToolType.Skill, skillDirectory: first.Directory!);
        var oldPhysical = PhysicalOf(first.Directory!);
        var newPhysical = PhysicalOf(second.Directory!);
        Assert.True(Directory.Exists(oldPhysical));

        await _host.PublishAsync(new UpdateToolCommand(toolId, new UpdateToolDto {
            Code = $"tool-{Guid.NewGuid():N}",
            Name = "改名",

            ToolPackageId = packageId,
            Setting = new SkillToolSetting { Directory = second.Directory! },
        }));

        Assert.False(Directory.Exists(oldPhysical));
        Assert.True(Directory.Exists(newPhysical));
    }

    [Fact]
    public async Task UpdateSkillTool_SameDirectory_KeepsDirectory()
    {
        var categoryId = await TestDataFactory.CreateCategoryAsync(_host, $"技能分类-{Guid.NewGuid():N}");
        var packageId = await TestDataFactory.CreatePackageAsync(_host, categoryId);

        var upload = new UploadSkillPackageCommand(StreamOf(ZipHelper.CreateSkillPackage()));
        await _host.PublishAsync(upload);

        var toolId = await TestDataFactory.CreateToolAsync(_host, packageId, type: ToolType.Skill, skillDirectory: upload.Directory!);
        var physical = PhysicalOf(upload.Directory!);

        await _host.PublishAsync(new UpdateToolCommand(toolId, new UpdateToolDto {
            Code = $"tool-{Guid.NewGuid():N}",
            Name = "改名",

            ToolPackageId = packageId,
            Setting = new SkillToolSetting { Directory = upload.Directory! },
        }));

        Assert.True(Directory.Exists(physical));
    }

    [Fact]
    public async Task Delete_UnknownOrForeignDirectory_DoesNotThrow_AndDeletesNothing()
    {
        var upload = new UploadSkillPackageCommand(StreamOf(ZipHelper.CreateSkillPackage()));
        await _host.PublishAsync(upload);
        var physical = PhysicalOf(upload.Directory!);
        Assert.True(Directory.Exists(physical));

        using var scope = _host.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<ISkillPackageStorage>();

        // 不存在 / 根目录 / 租户目录 / 逃逸路径：静默
        await storage.DeleteAsync("skills/0/not-exist", 0);
        await storage.DeleteAsync("skills/", 0);
        await storage.DeleteAsync("skills/0/", 0);
        await storage.DeleteAsync("skills/0/../1", 0);
        await storage.DeleteAsync("outside/0/escape", 0);
        // 跨租户：租户 1 不能删租户 0 的目录
        await storage.DeleteAsync(upload.Directory!, 1);
        Assert.True(Directory.Exists(physical));

        // 归属租户自己：删除
        await storage.DeleteAsync(upload.Directory!, 0);
        Assert.False(Directory.Exists(physical));
    }
}
