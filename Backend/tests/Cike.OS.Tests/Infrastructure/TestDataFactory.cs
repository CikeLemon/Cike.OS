using Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;
using Cike.OS.Application.Contracts.ToolPackages.Dtos;
using Cike.OS.Application.Contracts.Tools.Dtos;
using Cike.OS.Application.ToolPackageCategories.Commands;
using Cike.OS.Application.ToolPackages.Commands;
using Cike.OS.Application.Tools.Commands;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Tests.Infrastructure;

/// <summary>通过发布 Command 的方式造测试数据，走与生产一致的用例链路。</summary>
public static class TestDataFactory
{
    public static async Task<long> CreateCategoryAsync(CikeOSTestHost host, string name)
    {
        var command = new CreateToolPackageCategoryCommand(new CreateToolPackageCategoryDto { Name = name });
        await host.PublishAsync(command);
        return command.Id;
    }

    public static async Task<long> CreatePackageAsync(CikeOSTestHost host, long categoryId, string? code = null, ToolPackageStatus status = ToolPackageStatus.Draft, string name = "测试工具包")
    {
        var command = new CreateToolPackageCommand(new CreateToolPackageDto
        {
            Code = code ?? $"pkg-{Guid.NewGuid():N}",
            Name = name,
            ToolPackageCategoryId = categoryId,
            Status = status,
        });
        await host.PublishAsync(command);
        return command.Id;
    }

    public static async Task<long> CreateToolAsync(CikeOSTestHost host, long packageId, string? code = null, ToolType type = ToolType.HttpMcp, string skillDirectory = "skills/0/placeholder")
    {
        var command = new CreateToolCommand(new CreateToolDto
        {
            Code = code ?? $"tool-{Guid.NewGuid():N}",
            Name = "测试工具",
            Type = type,
            ToolPackageId = packageId,
            Setting = CreateSetting(type, skillDirectory),
        });
        await host.PublishAsync(command);
        return command.Id;
    }

    public static ToolSetting CreateSetting(ToolType type, string skillDirectory = "skills/0/placeholder") => type switch
    {
        ToolType.Skill => new SkillToolSetting { Directory = skillDirectory },
        ToolType.StdioMcp => new StdioMcpToolSetting { Name = "stdio", Command = "node" },
        _ => new HttpMcpToolSetting { Name = "http", Endpoint = "http://localhost:5000/mcp" },
    };
}
