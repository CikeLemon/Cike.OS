using Cike.OS.Application.Contracts.Agents.Dtos;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.Contracts.Tags.Dtos;
using Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;
using Cike.OS.Application.Contracts.ToolPackages.Dtos;
using Cike.OS.Application.Contracts.Tools.Dtos;
using Cike.OS.Application.Agents.Commands;
using Cike.OS.Application.Agents.Queries;
using Cike.OS.Application.ModelProviders.Commands;
using Cike.OS.Application.ModelProviders.Queries;
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

    public static async Task<long> CreateToolAsync(CikeOSTestHost host, long packageId, string? code = null, ToolType type = ToolType.HttpMcp, string skillDirectory = "skills/0/placeholder", List<ToolParameterDefinition>? parameters = null, string name = "测试工具")
    {
        var command = new CreateToolCommand(new CreateToolDto
        {
            Code = code ?? $"tool-{Guid.NewGuid():N}",
            Name = name,
            Type = type,
            ToolPackageId = packageId,
            Parameters = parameters ?? [],
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

    private static string NewName() => $"provider-{Guid.NewGuid():N}";

    public static ModelProviderSetting CreateSetting(ModelProviderProtocol protocol) => protocol switch
    {
        ModelProviderProtocol.Anthropic => new AnthropicModelProviderSetting { Version = "2023-06-01" },
        ModelProviderProtocol.OpenAIResponse => new OpenAIResponseModelProviderSetting { Organization = "org-1" },
        _ => new OpenAIChatCompletionModelProviderSetting { Organization = "org-1" },
    };

    /// <summary>模型提供方的新建输入：连接字段 + 它提供的全部模型。名称不传时随机（同租户内不重名，避免用例间互撞）。</summary>
    public static CreateModelProviderDto Provider(
        string? name = null,
        ModelProviderProtocol protocol = ModelProviderProtocol.OpenAIChatCompletion,
        IReadOnlyList<SaveModelProviderModelDto>? models = null,
        string baseUrl = "https://api.openai.com/v1",
        string apiKey = "sk-test",
        ModelProviderSetting? setting = null) => new()
    {
        Name = name ?? NewName(),
        Protocol = protocol,
        BaseUrl = baseUrl,
        ApiKey = apiKey,
        Setting = setting ?? CreateSetting(protocol),
        Models = models?.ToList() ?? [],
    };

    /// <summary>模型提供方的更新输入：连接字段与模型列表同新建（协议可改）。名称不传时随机。</summary>
    public static UpdateModelProviderDto ProviderUpdate(
        string? name = null,
        ModelProviderProtocol protocol = ModelProviderProtocol.OpenAIChatCompletion,
        IReadOnlyList<SaveModelProviderModelDto>? models = null,
        string baseUrl = "https://api.openai.com/v1",
        string apiKey = "sk-test",
        ModelProviderSetting? setting = null) => new()
    {
        Name = name ?? NewName(),
        Protocol = protocol,
        BaseUrl = baseUrl,
        ApiKey = apiKey,
        Setting = setting ?? CreateSetting(protocol),
        Models = models?.ToList() ?? [],
    };

    /// <summary>模型的一条输入：以名称为键，不带 Id。</summary>
    public static SaveModelProviderModelDto Model(string name, string? description = null, List<long>? tagIds = null) => new()
    {
        Name = name,
        Description = description,
        TagIds = tagIds ?? [],
    };

    /// <summary>只建提供方（一条连接，没有任何模型）。</summary>
    public static async Task<long> CreateModelProviderAsync(CikeOSTestHost host, string name)
    {
        var command = new CreateModelProviderCommand(Provider(name));
        await host.PublishAsync(command);
        return command.Id;
    }

    public static Task UpdateModelProviderAsync(CikeOSTestHost host, long providerId, UpdateModelProviderDto provider) =>
        host.PublishAsync(new UpdateModelProviderCommand(providerId, provider));

    /// <summary>读回一个提供方（连接字段 + 按名称排序的全部模型）；不存在时为 null。</summary>
    public static async Task<ModelProviderDetailDto?> GetModelProviderAsync(CikeOSTestHost host, long providerId)
    {
        var query = new GetModelProviderDetailQuery(providerId);
        await host.PublishAsync(query);
        return query.Result;
    }

    public static async Task<long> CreateTagAsync(CikeOSTestHost host, string key, string name = "测试标签")
    {
        var command = new CreateTagCommand(new CreateTagDto { Key = key, Name = name });
        await host.PublishAsync(command);
        return command.Id;
    }

    private static string NewAgentName() => $"agent-{Guid.NewGuid():N}";

    public static SaveAgentToolDto AgentTool(AgentToolType type, long targetId) => new() { Type = type, TargetId = targetId };

    /// <summary>带一个必填敏感参数的工具（供用户工具配置与 Agent 跳转规则的用例造数）。</summary>
    public static Task<long> CreateToolWithParametersAsync(CikeOSTestHost host, long packageId, string? name = null) =>
        CreateToolAsync(host, packageId, name: name ?? $"带参数工具-{Guid.NewGuid():N}", parameters:
        [
            new ToolParameterDefinition { Code = "ak", Name = "Access key", Required = true, Sensitive = true },
        ]);

    /// <summary>Agent 的新建输入：名称不传时随机（同租户内不重名，避免用例间互撞）。</summary>
    public static CreateAgentDto Agent(
        string? name = null,
        string? description = null,
        string instructions = "You are a helpful assistant.",
        AgentOptions? options = null,
        IReadOnlyList<SaveAgentToolDto>? tools = null) => new()
    {
        Name = name ?? NewAgentName(),
        Description = description,
        Instructions = instructions,
        Options = options ?? new AgentOptions(),
        Tools = tools?.ToList() ?? [],
    };

    /// <summary>Agent 的更新输入：与新建同形（没有不可变字段）。</summary>
    public static UpdateAgentDto AgentUpdate(CreateAgentDto dto) => new()
    {
        Name = dto.Name,
        Description = dto.Description,
        Instructions = dto.Instructions,
        Options = dto.Options,
        Tools = dto.Tools,
    };

    public static async Task<long> CreateAgentAsync(CikeOSTestHost host, CreateAgentDto? dto = null, IReadOnlyList<SaveAgentToolDto>? tools = null)
    {
        var command = new CreateAgentCommand(dto ?? Agent(tools: tools));
        await host.PublishAsync(command);
        return command.Id;
    }

    public static Task UpdateAgentAsync(CikeOSTestHost host, long agentId, UpdateAgentDto dto) =>
        host.PublishAsync(new UpdateAgentCommand(agentId, dto));

    /// <summary>读回一个 Agent（含工具条目）；不存在时为 null。</summary>
    public static async Task<AgentDetailDto?> GetAgentAsync(CikeOSTestHost host, long agentId)
    {
        var query = new GetAgentDetailQuery(agentId);
        await host.PublishAsync(query);
        return query.Result;
    }
}
