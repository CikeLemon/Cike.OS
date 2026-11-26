using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.Contracts.Models.Dtos;
using Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;
using Cike.OS.Application.Contracts.ToolPackages.Dtos;
using Cike.OS.Application.Contracts.Tools.Dtos;
using Cike.OS.Application.ModelProviders.Commands;
using Cike.OS.Application.Models.Commands;
using Cike.OS.Application.ToolPackageCategories.Commands;
using Cike.OS.Application.ToolPackages.Commands;
using Cike.OS.Application.Tools.Commands;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    public static ModelProviderSetting CreateProviderSetting(ModelProviderType type = ModelProviderType.OpenAI) => type switch
    {
        ModelProviderType.Anthropic => new AnthropicModelProviderSetting
        {
            Type = ModelProviderType.Anthropic,
            BaseUrl = "https://api.anthropic.com",
            ApiKey = "ak-test",
            Version = "2023-06-01",
        },
        _ => new OpenAIModelProviderSetting
        {
            Type = ModelProviderType.OpenAI,
            BaseUrl = "https://api.openai.com/v1",
            ApiKey = "sk-test",
            Organization = "org-1",
        },
    };

    public static async Task<long> CreateModelProviderAsync(
        CikeOSTestHost host,
        string name,
        ModelProviderType type = ModelProviderType.OpenAI,
        IReadOnlyList<SaveModelProviderMappingDto>? mappings = null)
    {
        var command = new CreateModelProviderCommand(new CreateModelProviderDto
        {
            Name = name,
            Setting = CreateProviderSetting(type),
            Mappings = mappings?.ToList() ?? [],
        });
        await host.PublishAsync(command);
        return command.Id;
    }

    public static async Task<long> CreateModelAsync(CikeOSTestHost host, string name, string displayName, string? description = null, List<long>? tagIds = null)
    {
        var command = new CreateModelCommand(new CreateModelDto
        {
            Name = name,
            DisplayName = displayName,
            Description = description,
            TagIds = tagIds ?? [],
        });
        await host.PublishAsync(command);
        return command.Id;
    }

    public static SaveModelProviderMappingDto Mapping(long modelId, int priority = 10, string? providerModelName = null) => new()
    {
        ModelId = modelId,
        Priority = priority,
        ProviderModelName = providerModelName,
    };

    /// <summary>标签暂无写侧用例（只读端点），测试直接走 DbContext 造数据。</summary>
    public static async Task<long> CreateTagAsync(CikeOSTestHost host, string key, string name)
    {
        using var scope = host.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();
        var tag = new Cike.OS.Domain.Entities.Tag { Key = key, Name = name };
        dbContext.Add(tag);
        await dbContext.SaveChangesAsync();
        return tag.Id;
    }
}
