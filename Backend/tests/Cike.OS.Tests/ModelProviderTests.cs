using Cike.OS.Application.ModelProviderEndpoints.Queries;
using Cike.OS.Application.ModelProviderEndpoints.Commands;
using Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;
using Cike.MultiTenancy;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Commands;
using Cike.OS.Application.ModelProviders.Queries;
using Cike.OS.Application.Models.Commands;
using Cike.OS.Application.Models.Queries;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cike.OS.Tests;

/// <summary>
/// 模型提供方与接入点：业务规则从发布 Command / Query 进入。
/// 接入点是独立的聚合（按提供方 Id 归属），经它自己的用例增删改查；提供商映射随接入点整体同步；提供方只管自己的名称。
/// </summary>
public class ModelProviderTests : IClassFixture<CikeOSTestHost>
{
    private readonly CikeOSTestHost _host;

    public ModelProviderTests(CikeOSTestHost host)
    {
        _host = host;
    }

    private async Task WithTenantAsync(long tenantId, Func<Task> action)
    {
        using var scope = _host.CreateScope();
        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        using var _ = currentTenant.Change(tenantId);
        await action();
    }

    private async Task<ModelProviderDto> GetProviderAsync(long id)
    {
        var query = new GetModelProviderQuery(id);
        await _host.PublishAsync(query);
        return query.Result!;
    }

    private async Task<ModelProviderDto> CreateProviderAsync(string name)
    {
        var id = await TestDataFactory.CreateModelProviderAsync(_host, name);
        return await GetProviderAsync(id);
    }

    private Task<List<ModelProviderEndpointDto>> GetEndpointsAsync(long providerId) =>
        TestDataFactory.GetEndpointsAsync(_host, providerId);

    private Task<ModelProviderEndpointDto?> GetEndpointAsync(long endpointId) =>
        TestDataFactory.GetEndpointAsync(_host, endpointId);

    private Task<long> AddEndpointAsync(CreateModelProviderEndpointDto endpoint) =>
        TestDataFactory.AddEndpointAsync(_host, endpoint);

    private Task UpdateEndpointAsync(long endpointId, UpdateModelProviderEndpointDto endpoint) =>
        TestDataFactory.UpdateEndpointAsync(_host, endpointId, endpoint);

    private Task RemoveEndpointAsync(long endpointId) =>
        TestDataFactory.DeleteEndpointAsync(_host, endpointId);

    private Task RenameAsync(long providerId, string name) =>
        TestDataFactory.RenameModelProviderAsync(_host, providerId, name);

    // 显示名称全局唯一：造数时带上唯一后缀，避免用例间互撞
    private async Task<long> CreateModelAsync(string label) =>
        await TestDataFactory.CreateModelAsync(_host, $"m-{Guid.NewGuid():N}", $"模型{label}-{Guid.NewGuid():N}");

    private async Task AssertModelDeletableAsync(long modelId)
    {
        await _host.PublishAsync(new DeleteModelCommand(modelId));
        var model = new GetModelQuery(modelId);
        await _host.PublishAsync(model);
        Assert.Null(model.Result);
    }

    private async Task AssertModelDeleteRejectedAsync(long modelId)
    {
        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(new DeleteModelCommand(modelId)));
    }

    // ───── 模型提供方：只管自己的名称 ─────

    [Fact]
    public async Task Create_OnlyName_Get_ReturnsNameOnly()
    {
        var name = $"bare-{Guid.NewGuid():N}";
        var provider = await CreateProviderAsync(name);

        Assert.True(provider.Id > 0);
        Assert.Equal(name, provider.Name);
        Assert.Empty(await GetEndpointsAsync(provider.Id));
    }

    [Fact]
    public async Task Update_RenamesProvider_AndKeepsEndpoints()
    {
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"rename-{Guid.NewGuid():N}");
        var endpointId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId));

        await RenameAsync(providerId, "改名后的提供方");

        Assert.Equal("改名后的提供方", (await GetProviderAsync(providerId)).Name);
        Assert.Equal([endpointId], (await GetEndpointsAsync(providerId)).Select(x => x.Id).ToList());
    }

    [Fact]
    public async Task Update_DuplicateName_Throws()
    {
        var takenName = $"taken-{Guid.NewGuid():N}";
        await TestDataFactory.CreateModelProviderAsync(_host, takenName);
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"other-{Guid.NewGuid():N}");

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => RenameAsync(providerId, takenName));
        Assert.Contains("已存在", ex.Message);
    }

    [Fact]
    public async Task Update_NonexistentProvider_Throws()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => RenameAsync(999999, "幽灵"));
        Assert.Contains("模型提供方不存在", ex.Message);
    }

    [Fact]
    public async Task Create_DuplicateName_SameTenant_Throws()
    {
        var name = $"dup-{Guid.NewGuid():N}";
        await TestDataFactory.CreateModelProviderAsync(_host, name);

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => TestDataFactory.CreateModelProviderAsync(_host, name));
        Assert.Contains("已存在", ex.Message);
    }

    [Fact]
    public async Task Create_SameName_DifferentTenants_Succeeds()
    {
        var name = $"tenant-{Guid.NewGuid():N}";
        var firstId = await TestDataFactory.CreateModelProviderAsync(_host, name);

        long secondId = 0;
        await WithTenantAsync(1, async () =>
        {
            secondId = await TestDataFactory.CreateModelProviderAsync(_host, name);
        });

        Assert.NotEqual(firstId, secondId);
    }

    [Fact]
    public async Task List_OrdersByName()
    {
        await CreateProviderAsync($"b-{Guid.NewGuid():N}");
        await CreateProviderAsync($"a-{Guid.NewGuid():N}");

        var query = new GetModelProviderListQuery();
        await _host.PublishAsync(query);

        var names = query.Result!.Select(x => x.Name).ToList();
        Assert.True(names.Count >= 2);
        Assert.Equal(names.OrderBy(x => x, StringComparer.Ordinal), names);
    }

    [Fact]
    public async Task List_SummariesEachProvider_ProtocolsModelsAndEndpointCount()
    {
        var modelHigh = await CreateModelAsync("高分");
        var modelLow = await CreateModelAsync("低分");
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"summary-{Guid.NewGuid():N}");
        await AddEndpointAsync(TestDataFactory.Endpoint(providerId,
            "官方 Anthropic", ModelProviderProtocol.Anthropic, mappings: [TestDataFactory.Mapping(modelLow, 1)]));
        await AddEndpointAsync(TestDataFactory.Endpoint(providerId,
            "官方 OpenAI", mappings: [TestDataFactory.Mapping(modelHigh, 99), TestDataFactory.Mapping(modelLow, 50)]));
        await AddEndpointAsync(TestDataFactory.Endpoint(providerId, "中转", mappings: [TestDataFactory.Mapping(modelHigh, 5)]));

        var query = new GetModelProviderListQuery();
        await _host.PublishAsync(query);
        var row = query.Result!.Single(x => x.Id == providerId);

        // 协议去重并按枚举顺序；模型跨接入点去重、分数高的在前；接入点数是全部接入点
        Assert.Equal([ModelProviderProtocol.OpenAIChatCompletion, ModelProviderProtocol.Anthropic], row.Protocols);
        Assert.Equal(3, row.EndpointCount);
        Assert.True(row.ModelDisplayNames.Count == 2);
        Assert.Contains("高分", row.ModelDisplayNames[0]);
        Assert.Contains("低分", row.ModelDisplayNames[1]);
    }

    [Fact]
    public async Task Delete_RemovesProvider()
    {
        var provider = await CreateProviderAsync($"del-{Guid.NewGuid():N}");

        await _host.PublishAsync(new DeleteModelProviderCommand(provider.Id));

        var query = new GetModelProviderQuery(provider.Id);
        await _host.PublishAsync(query);
        Assert.Null(query.Result);
    }

    [Fact]
    public async Task Delete_NonexistentProvider_Throws()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(new DeleteModelProviderCommand(999999)));
        Assert.Contains("模型提供方不存在", ex.Message);
    }

    [Fact]
    public async Task Delete_Provider_RemovesEndpointsAndMappings_ThenModelCanBeDeleted()
    {
        var modelId = await CreateModelAsync("级联");
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"cascade-{Guid.NewGuid():N}");
        var endpointId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId, mappings: [TestDataFactory.Mapping(modelId)]));

        await _host.PublishAsync(new DeleteModelProviderCommand(providerId));

        // 名下的接入点与映射一并删除：列表为空、详情取不到、模型不再被引用可以直接删
        Assert.Empty(await GetEndpointsAsync(providerId));
        Assert.Null(await GetEndpointAsync(endpointId));
        await AssertModelDeletableAsync(modelId);
    }

    // ───── 接入点：独立的聚合 ─────

    [Fact]
    public async Task Create_TwoEndpointsWithDifferentProtocols_ReturnsConnectionAndProvider()
    {
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"two-{Guid.NewGuid():N}");
        var openAiId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId, "官方 OpenAI"));
        var anthropicId = await AddEndpointAsync(TestDataFactory.Endpoint(
            providerId, "官方 Anthropic", ModelProviderProtocol.Anthropic, baseUrl: "https://api.anthropic.com", apiKey: "ak-test"));

        var endpoints = await GetEndpointsAsync(providerId);

        // 接入点按名称排序；地址与密钥在接入点上，协议专属设置在 Setting 里
        Assert.Equal(["官方 Anthropic", "官方 OpenAI"], endpoints.Select(x => x.Name).ToList());

        var openAi = endpoints.Single(x => x.Id == openAiId);
        Assert.Equal(providerId, openAi.ModelProviderId);
        Assert.Equal(ModelProviderProtocol.OpenAIChatCompletion, openAi.Protocol);
        Assert.Equal("https://api.openai.com/v1", openAi.BaseUrl);
        Assert.Equal("sk-test", openAi.ApiKey);
        Assert.Equal("org-1", Assert.IsType<OpenAIChatCompletionModelProviderEndpointSetting>(openAi.Setting).Organization);

        var anthropic = endpoints.Single(x => x.Id == anthropicId);
        Assert.Equal(ModelProviderProtocol.Anthropic, anthropic.Protocol);
        Assert.Equal("https://api.anthropic.com", anthropic.BaseUrl);
        Assert.Equal("ak-test", anthropic.ApiKey);
        Assert.Equal("2023-06-01", Assert.IsType<AnthropicModelProviderEndpointSetting>(anthropic.Setting).Version);
    }

    [Fact]
    public async Task Create_OpenAIResponseEndpoint_StoresResponseSetting()
    {
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"response-{Guid.NewGuid():N}");
        await AddEndpointAsync(TestDataFactory.Endpoint(
            providerId, "官方 Response", ModelProviderProtocol.OpenAIResponse, baseUrl: "https://api.openai.com/v1"));

        var endpoint = Assert.Single(await GetEndpointsAsync(providerId));
        Assert.Equal(ModelProviderProtocol.OpenAIResponse, endpoint.Protocol);
        Assert.Equal("org-1", Assert.IsType<OpenAIResponseModelProviderEndpointSetting>(endpoint.Setting).Organization);
    }

    [Fact]
    public async Task Create_WithoutProtocolSpecificFields_StoresNull()
    {
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"nulls-{Guid.NewGuid():N}");
        await AddEndpointAsync(TestDataFactory.Endpoint(providerId, setting: new OpenAIChatCompletionModelProviderEndpointSetting()));

        var endpoint = Assert.Single(await GetEndpointsAsync(providerId));
        Assert.Null(Assert.IsType<OpenAIChatCompletionModelProviderEndpointSetting>(endpoint.Setting).Organization);
    }

    [Fact]
    public async Task Create_OnNonexistentProvider_Throws()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => AddEndpointAsync(TestDataFactory.Endpoint(999999)));
        Assert.Contains("模型提供方不存在", ex.Message);
    }

    [Fact]
    public async Task GetList_OnNonexistentProvider_ReturnsEmpty()
    {
        Assert.Empty(await GetEndpointsAsync(999999));
    }

    [Fact]
    public async Task Get_OnNonexistentEndpoint_ReturnsNull()
    {
        Assert.Null(await GetEndpointAsync(999999));
    }

    [Fact]
    public async Task Get_ReturnsDetail_WithMappingsByScoreDescAndModelName()
    {
        var modelA = await CreateModelAsync("甲");
        var modelB = await CreateModelAsync("乙");
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"detail-{Guid.NewGuid():N}");
        var endpointId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId,
            "中转", mappings: [TestDataFactory.Mapping(modelA, 10), TestDataFactory.Mapping(modelB, 30, "zhipu/glm")]));

        var endpoint = (await GetEndpointAsync(endpointId))!;

        Assert.Equal(endpointId, endpoint.Id);
        Assert.Equal(providerId, endpoint.ModelProviderId);
        Assert.Equal("中转", endpoint.Name);
        Assert.Equal([modelB, modelA], endpoint.Mappings.Select(x => x.ModelId).ToList());
        Assert.Contains("模型乙", endpoint.Mappings[0].ModelDisplayName);
        Assert.Equal("zhipu/glm", endpoint.Mappings[0].ProviderModelName);
    }

    [Fact]
    public async Task Update_Endpoint_ChangesConnectionAndSetting_KeepsProtocolAndSiblings()
    {
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"upd-ep-{Guid.NewGuid():N}");
        var endpointId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId));
        var siblingId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId, "中转", apiKey: "sk-sibling"));

        await UpdateEndpointAsync(endpointId, TestDataFactory.EndpointUpdate(
            name: "官方（改）", baseUrl: "https://vip.api.openai.com/v1", apiKey: "sk-new"));

        var endpoint = (await GetEndpointAsync(endpointId))!;
        Assert.Equal("官方（改）", endpoint.Name);
        Assert.Equal("https://vip.api.openai.com/v1", endpoint.BaseUrl);
        Assert.Equal("sk-new", endpoint.ApiKey);

        // 协议与归属创建后不可变：更新请求里没有它们，保存后仍是原值
        Assert.Equal(ModelProviderProtocol.OpenAIChatCompletion, endpoint.Protocol);
        Assert.Equal(providerId, endpoint.ModelProviderId);

        // 只动这一个接入点：其余接入点原样
        var sibling = (await GetEndpointsAsync(providerId)).Single(x => x.Id == siblingId);
        Assert.Equal("中转", sibling.Name);
        Assert.Equal("sk-sibling", sibling.ApiKey);
    }

    [Fact]
    public async Task Update_EndpointWithSettingOfAnotherProtocol_Throws_AndChangesNothing()
    {
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"mismatch-{Guid.NewGuid():N}");
        var endpointId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId));

        // 协议专属设置的子类型必须与接入点现有协议一致
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => UpdateEndpointAsync(
            endpointId, TestDataFactory.EndpointUpdate(name: "改不动", setting: new AnthropicModelProviderEndpointSetting())));
        Assert.Contains("协议与设置类型不匹配", ex.Message);

        Assert.Equal("官方", (await GetEndpointAsync(endpointId))!.Name);
    }

    [Fact]
    public async Task Update_WithUnknownEndpointId_Throws()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => UpdateEndpointAsync(999999, TestDataFactory.EndpointUpdate("幽灵")));
        Assert.Contains("接入点不存在", ex.Message);
    }

    [Fact]
    public async Task Delete_WithUnknownEndpointId_Throws()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => RemoveEndpointAsync(999999));
        Assert.Contains("接入点不存在", ex.Message);
    }

    [Fact]
    public async Task Create_DuplicateEndpointName_SameProvider_Throws_AndSavesNothing()
    {
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"same-ep-{Guid.NewGuid():N}");
        await AddEndpointAsync(TestDataFactory.Endpoint(providerId, "官方"));

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => AddEndpointAsync(TestDataFactory.Endpoint(providerId, "官方")));
        Assert.Contains("接入点", ex.Message);

        Assert.Single(await GetEndpointsAsync(providerId));
    }

    [Fact]
    public async Task Update_EndpointNameToSiblingName_Throws_AndChangesNothing()
    {
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"sibling-{Guid.NewGuid():N}");
        await AddEndpointAsync(TestDataFactory.Endpoint(providerId, "官方"));
        var endpointId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId, "中转"));

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => UpdateEndpointAsync(endpointId, TestDataFactory.EndpointUpdate("官方")));
        Assert.Contains("接入点", ex.Message);

        Assert.Equal(["官方", "中转"], (await GetEndpointsAsync(providerId)).Select(x => x.Name).ToList());
    }

    [Fact]
    public async Task Create_SameEndpointName_DifferentProviders_Succeeds()
    {
        var firstId = await TestDataFactory.CreateModelProviderAsync(_host, $"ep-a-{Guid.NewGuid():N}");
        var secondId = await TestDataFactory.CreateModelProviderAsync(_host, $"ep-b-{Guid.NewGuid():N}");

        await AddEndpointAsync(TestDataFactory.Endpoint(firstId, "官方"));
        await AddEndpointAsync(TestDataFactory.Endpoint(secondId, "官方"));

        Assert.Single(await GetEndpointsAsync(firstId));
        Assert.Single(await GetEndpointsAsync(secondId));
    }

    [Fact]
    public async Task Create_SameName_AfterEndpointDeleted_Succeeds()
    {
        // 软删过滤生效：删掉后同名可重建（索引非唯一，见 ADR 0001）
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"recreate-{Guid.NewGuid():N}");
        var endpointId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId, "官方"));

        await RemoveEndpointAsync(endpointId);
        var recreatedId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId, "官方"));

        Assert.Equal([recreatedId], (await GetEndpointsAsync(providerId)).Select(x => x.Id).ToList());
    }

    [Fact]
    public async Task Delete_LastEndpoint_LeavesProvider()
    {
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"last-{Guid.NewGuid():N}");
        var endpointId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId));

        await RemoveEndpointAsync(endpointId);

        Assert.Empty(await GetEndpointsAsync(providerId));
        Assert.NotNull((await GetProviderAsync(providerId)).Id);
    }

    [Fact]
    public async Task Endpoints_OfAnotherTenant_AreInvisible_AndCannotBeTakenOver()
    {
        long tenant1ProviderId = 0;
        long tenant1EndpointId = 0;
        await WithTenantAsync(1, async () =>
        {
            tenant1ProviderId = await TestDataFactory.CreateModelProviderAsync(_host, $"租户一提供商-{Guid.NewGuid():N}");
            tenant1EndpointId = await AddEndpointAsync(TestDataFactory.Endpoint(tenant1ProviderId));
        });

        // 租户 0 看不到租户 1 的提供方与接入点：新建按「提供方不存在」被拒，修改 / 删除按「接入点不存在」被拒，读取为空
        var createEx = await Assert.ThrowsAsync<UserFriendlyException>(
            () => AddEndpointAsync(TestDataFactory.Endpoint(tenant1ProviderId)));
        Assert.Contains("模型提供方不存在", createEx.Message);

        var updateEx = await Assert.ThrowsAsync<UserFriendlyException>(
            () => UpdateEndpointAsync(tenant1EndpointId, TestDataFactory.EndpointUpdate("抢占")));
        Assert.Contains("接入点不存在", updateEx.Message);

        var deleteEx = await Assert.ThrowsAsync<UserFriendlyException>(() => RemoveEndpointAsync(tenant1EndpointId));
        Assert.Contains("接入点不存在", deleteEx.Message);

        Assert.Empty(await GetEndpointsAsync(tenant1ProviderId));
        Assert.Null(await GetEndpointAsync(tenant1EndpointId));

        await WithTenantAsync(1, async () =>
        {
            Assert.Equal("官方", (await GetEndpointAsync(tenant1EndpointId))!.Name);
        });
    }

    // ───── 提供商映射：随接入点 ─────

    [Fact]
    public async Task Create_WithMappings_ReturnsThemByScoreDesc_WithModelName()
    {
        var modelA = await CreateModelAsync("甲");
        var modelB = await CreateModelAsync("乙");
        var modelC = await CreateModelAsync("丙");
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"map-{Guid.NewGuid():N}");

        var endpointId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId, mappings:
        [
            TestDataFactory.Mapping(modelA, 10),
            TestDataFactory.Mapping(modelB, 30, "zhipu/glm-5.3"),
            TestDataFactory.Mapping(modelC, 10),
        ]));

        var endpoint = (await GetEndpointsAsync(providerId)).Single(x => x.Id == endpointId);

        // 分数高的排前面；分数只是排序依据，不要求唯一（甲、丙同为 10）
        Assert.Equal([modelB, modelA, modelC], endpoint.Mappings.Select(x => x.ModelId).ToList());
        Assert.Equal([30, 10, 10], endpoint.Mappings.Select(x => x.Priority).ToList());

        // 模型显示名称由查询侧回填；未填提供商侧模型名时为空字符串（表示回落模型名称）
        var top = endpoint.Mappings[0];
        Assert.Contains("模型乙", top.ModelDisplayName);
        Assert.Equal("zhipu/glm-5.3", top.ProviderModelName);
        Assert.Equal(string.Empty, endpoint.Mappings[1].ProviderModelName);
    }

    [Fact]
    public async Task Create_SameModelTwice_InOneEndpoint_Throws_AndSavesNothing()
    {
        var modelId = await CreateModelAsync("重复");
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"twice-{Guid.NewGuid():N}");

        // 一个接入点对一个模型只保留一条映射：聚合内部不变量
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => AddEndpointAsync(TestDataFactory.Endpoint(providerId, mappings:
        [
            TestDataFactory.Mapping(modelId, 10),
            TestDataFactory.Mapping(modelId, 20),
        ])));
        Assert.Contains("只能映射一次", ex.Message);

        Assert.Empty(await GetEndpointsAsync(providerId));
    }

    [Fact]
    public async Task Create_SameModel_InTwoEndpointsOfOneProvider_Succeeds()
    {
        // 同一个模型既走 OpenAI 兼容、又走 Anthropic 兼容：映射归接入点，跨接入点不重复受限
        var modelId = await CreateModelAsync("双协议");
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"dual-{Guid.NewGuid():N}");

        await AddEndpointAsync(TestDataFactory.Endpoint(providerId, "官方 OpenAI", mappings: [TestDataFactory.Mapping(modelId)]));
        await AddEndpointAsync(TestDataFactory.Endpoint(
            providerId, "官方 Anthropic", ModelProviderProtocol.Anthropic, mappings: [TestDataFactory.Mapping(modelId, providerModelName: "glm-5.3")]));

        var endpoints = await GetEndpointsAsync(providerId);
        Assert.Equal(2, endpoints.Count);
        Assert.All(endpoints, x => Assert.Equal([modelId], x.Mappings.Select(m => m.ModelId).ToList()));
    }

    [Fact]
    public async Task SameModelAndScore_AcrossProviders_IsAllowed()
    {
        var modelId = await CreateModelAsync("同分");
        var firstId = await TestDataFactory.CreateModelProviderAsync(_host, $"same-a-{Guid.NewGuid():N}");
        var secondId = await TestDataFactory.CreateModelProviderAsync(_host, $"same-b-{Guid.NewGuid():N}");

        await AddEndpointAsync(TestDataFactory.Endpoint(firstId, mappings: [TestDataFactory.Mapping(modelId, 10)]));
        await AddEndpointAsync(TestDataFactory.Endpoint(secondId, mappings: [TestDataFactory.Mapping(modelId, 10)]));

        // 同一个模型被两个提供方以相同分数接入：路由时再按分数排序取第一个
        Assert.Single(Assert.Single(await GetEndpointsAsync(firstId)).Mappings);
        Assert.Single(Assert.Single(await GetEndpointsAsync(secondId)).Mappings);
    }

    [Fact]
    public async Task Create_WithNonexistentModel_Throws_AndSavesNothing()
    {
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"missing-{Guid.NewGuid():N}");

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => AddEndpointAsync(TestDataFactory.Endpoint(providerId, mappings: [TestDataFactory.Mapping(999999)])));
        Assert.Contains("模型不存在", ex.Message);

        Assert.Empty(await GetEndpointsAsync(providerId));
    }

    [Fact]
    public async Task Update_WithNonexistentModel_Throws_AndChangesNothing()
    {
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"missing-upd-{Guid.NewGuid():N}");
        var endpointId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId));

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => UpdateEndpointAsync(endpointId, TestDataFactory.EndpointUpdate("改不动", mappings: [TestDataFactory.Mapping(999999)])));
        Assert.Contains("模型不存在", ex.Message);

        Assert.Equal("官方", (await GetEndpointAsync(endpointId))!.Name);
    }

    [Fact]
    public async Task Update_SyncsMappings_UpdatesKeepsAddsRemoves()
    {
        var modelA = await CreateModelAsync("留");
        var modelB = await CreateModelAsync("删");
        var modelC = await CreateModelAsync("增");
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"sync-{Guid.NewGuid():N}");
        var endpointId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId, mappings:
        [
            TestDataFactory.Mapping(modelA, 10),
            TestDataFactory.Mapping(modelB, 20),
        ]));

        await UpdateEndpointAsync(endpointId, TestDataFactory.EndpointUpdate(mappings:
        [
            TestDataFactory.Mapping(modelA, 99, "renamed-model"),
            TestDataFactory.Mapping(modelC, 5),
        ]));

        var endpoint = (await GetEndpointAsync(endpointId))!;

        // 甲更新、丙新增、乙移除
        Assert.Equal([modelA, modelC], endpoint.Mappings.Select(x => x.ModelId).ToList());
        Assert.Equal(99, endpoint.Mappings[0].Priority);
        Assert.Equal("renamed-model", endpoint.Mappings[0].ProviderModelName);

        // 被移除的映射不再挡住模型删除
        await AssertModelDeletableAsync(modelB);
    }

    [Fact]
    public async Task Delete_Endpoint_RemovesItsMappings_ThenModelCanBeDeleted()
    {
        var modelId = await CreateModelAsync("接入点级联");
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"ep-del-{Guid.NewGuid():N}");
        var endpointId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId, mappings: [TestDataFactory.Mapping(modelId)]));

        await RemoveEndpointAsync(endpointId);

        // 该接入点的映射一并删除：模型不再被引用，可以直接删
        Assert.Empty(await GetEndpointsAsync(providerId));
        await AssertModelDeletableAsync(modelId);
    }

    [Fact]
    public async Task Delete_OneEndpoint_KeepsSiblingsMappings()
    {
        var modelId = await CreateModelAsync("保留兄弟");
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"keep-sibling-{Guid.NewGuid():N}");
        var doomedId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId, "甲"));
        var siblingId = await AddEndpointAsync(TestDataFactory.Endpoint(providerId, "乙", mappings: [TestDataFactory.Mapping(modelId)]));

        await RemoveEndpointAsync(doomedId);

        // 只删这一个：兄弟接入点与它的映射原样，模型仍被引用、删除被拒
        var sibling = (await GetEndpointAsync(siblingId))!;
        Assert.Equal("乙", sibling.Name);
        Assert.Equal([modelId], sibling.Mappings.Select(x => x.ModelId).ToList());
        await AssertModelDeleteRejectedAsync(modelId);
    }

    [Fact]
    public async Task Mappings_AreIsolatedPerTenant()
    {
        var modelId = await CreateModelAsync("隔离");

        long tenant1ProviderId = 0;
        await WithTenantAsync(1, async () =>
        {
            tenant1ProviderId = await TestDataFactory.CreateModelProviderAsync(_host, $"租户一提供商-{Guid.NewGuid():N}");
            await AddEndpointAsync(TestDataFactory.Endpoint(tenant1ProviderId, mappings: [TestDataFactory.Mapping(modelId)]));
            Assert.Single(Assert.Single(await GetEndpointsAsync(tenant1ProviderId)).Mappings);
        });

        // 租户 0 的提供商列表里没有别家租户的提供方，自然也看不到它的接入点与映射
        var list = new GetModelProviderListQuery();
        await _host.PublishAsync(list);
        Assert.DoesNotContain(list.Result!, x => x.Id == tenant1ProviderId);
    }
}
