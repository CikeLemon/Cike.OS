using Cike.OS.Application.ModelProviderEndpoints.Queries;
using Cike.OS.Application.ModelProviderEndpoints.Commands;
using Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.Contracts.Models.Dtos;
using Cike.OS.Application.Contracts.Tags.Dtos;
using Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;
using Cike.OS.Application.Contracts.ToolPackages.Dtos;
using Cike.OS.Application.Contracts.Tools.Dtos;
using Cike.OS.Application.ModelProviders.Commands;
using Cike.OS.Application.ModelProviders.Queries;
using Cike.OS.Application.Models.Commands;
using Cike.OS.Application.Tags.Commands;
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

    /// <summary>只建提供方（只有名称，没有任何接入点）。</summary>
    public static async Task<long> CreateModelProviderAsync(CikeOSTestHost host, string name)
    {
        var command = new CreateModelProviderCommand(new CreateModelProviderDto { Name = name });
        await host.PublishAsync(command);
        return command.Id;
    }

    public static ModelProviderEndpointSetting CreateEndpointSetting(ModelProviderProtocol protocol) => protocol switch
    {
        ModelProviderProtocol.Anthropic => new AnthropicModelProviderEndpointSetting { Version = "2023-06-01" },
        ModelProviderProtocol.OpenAIResponse => new OpenAIResponseModelProviderEndpointSetting { Organization = "org-1" },
        _ => new OpenAIChatCompletionModelProviderEndpointSetting { Organization = "org-1" },
    };

    /// <summary>接入点的新建输入：第一个参数是所属提供方。</summary>
    public static CreateModelProviderEndpointDto Endpoint(
        long modelProviderId = 0,
        string name = "官方",
        ModelProviderProtocol protocol = ModelProviderProtocol.OpenAIChatCompletion,
        IReadOnlyList<SaveModelProviderMappingDto>? mappings = null,
        string baseUrl = "https://api.openai.com/v1",
        string apiKey = "sk-test",
        ModelProviderEndpointSetting? setting = null) => new()
    {
        ModelProviderId = modelProviderId,
        Name = name,
        Protocol = protocol,
        BaseUrl = baseUrl,
        ApiKey = apiKey,
        Setting = setting ?? CreateEndpointSetting(protocol),
        Mappings = mappings?.ToList() ?? [],
    };

    /// <summary>接入点的更新输入：没有协议（创建后不可变），设置的子类型要对着现有协议给。</summary>
    public static UpdateModelProviderEndpointDto EndpointUpdate(
        string name = "官方",
        ModelProviderProtocol protocol = ModelProviderProtocol.OpenAIChatCompletion,
        IReadOnlyList<SaveModelProviderMappingDto>? mappings = null,
        string baseUrl = "https://api.openai.com/v1",
        string apiKey = "sk-test",
        ModelProviderEndpointSetting? setting = null) => new()
    {
        Name = name,
        BaseUrl = baseUrl,
        ApiKey = apiKey,
        Setting = setting ?? CreateEndpointSetting(protocol),
        Mappings = mappings?.ToList() ?? [],
    };

    /// <summary>新建一个接入点（含它的全部映射），返回新接入点的 Id。</summary>
    public static async Task<long> AddEndpointAsync(CikeOSTestHost host, CreateModelProviderEndpointDto endpoint)
    {
        var command = new CreateModelProviderEndpointCommand(endpoint);
        await host.PublishAsync(command);
        return command.Id;
    }

    public static Task UpdateEndpointAsync(CikeOSTestHost host, long endpointId, UpdateModelProviderEndpointDto endpoint) =>
        host.PublishAsync(new UpdateModelProviderEndpointCommand(endpointId, endpoint));

    public static Task DeleteEndpointAsync(CikeOSTestHost host, long endpointId) =>
        host.PublishAsync(new DeleteModelProviderEndpointCommand(endpointId));

    /// <summary>读回一个提供方名下的全部接入点（按名称排序、映射按分数降序、带模型显示名）。</summary>
    public static async Task<List<ModelProviderEndpointDto>> GetEndpointsAsync(CikeOSTestHost host, long providerId)
    {
        var query = new GetModelProviderEndpointListQuery(providerId);
        await host.PublishAsync(query);
        return query.Result!;
    }

    /// <summary>读回一个接入点的详情；不存在时为 null。</summary>
    public static async Task<ModelProviderEndpointDto?> GetEndpointAsync(CikeOSTestHost host, long endpointId)
    {
        var query = new GetModelProviderEndpointQuery(endpointId);
        await host.PublishAsync(query);
        return query.Result;
    }

    public static Task RenameModelProviderAsync(CikeOSTestHost host, long providerId, string name) =>
        host.PublishAsync(new UpdateModelProviderCommand(providerId, new UpdateModelProviderDto { Name = name }));

    /// <summary>建一个「提供方 + 一个接入点 + 映射」，给只关心「模型是否被映射引用」的用例用。</summary>
    public static async Task CreateProviderWithMappingsAsync(CikeOSTestHost host, string name, IReadOnlyList<SaveModelProviderMappingDto> mappings)
    {
        var providerId = await CreateModelProviderAsync(host, name);
        await AddEndpointAsync(host, Endpoint(providerId, mappings: mappings));
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

    public static async Task<long> CreateTagAsync(CikeOSTestHost host, string key, string name = "测试标签")
    {
        var command = new CreateTagCommand(new CreateTagDto { Key = key, Name = name });
        await host.PublishAsync(command);
        return command.Id;
    }
}
