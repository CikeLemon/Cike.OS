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

    private async Task<ModelProviderDto> CreateProviderAsync(string name, ModelProviderType type = ModelProviderType.OpenAI)
    {
        var id = await TestDataFactory.CreateModelProviderAsync(_host, name, type);
        return await GetProviderAsync(id);
    }

    // 显示名称全局唯一：造数时带上唯一后缀，避免用例间互撞
    private async Task<long> CreateModelAsync(string label) =>
        await TestDataFactory.CreateModelAsync(_host, $"m-{Guid.NewGuid():N}", $"模型{label}-{Guid.NewGuid():N}");

    [Fact]
    public async Task Create_Then_Get_ReturnsOpenAISetting()
    {
        var provider = await CreateProviderAsync($"openai-{Guid.NewGuid():N}");

        var setting = Assert.IsType<OpenAIModelProviderSetting>(provider.Setting);
        Assert.Equal(ModelProviderType.OpenAI, setting.Type);
        Assert.Equal("https://api.openai.com/v1", setting.BaseUrl);
        Assert.Equal("sk-test", setting.ApiKey);
        Assert.Equal("org-1", setting.Organization);
        Assert.True(provider.Id > 0);
        Assert.Empty(provider.Mappings);
    }

    [Fact]
    public async Task Create_AnthropicSetting_RoundTrip()
    {
        var provider = await CreateProviderAsync($"anthropic-{Guid.NewGuid():N}", ModelProviderType.Anthropic);

        var setting = Assert.IsType<AnthropicModelProviderSetting>(provider.Setting);
        Assert.Equal(ModelProviderType.Anthropic, setting.Type);
        Assert.Equal("2023-06-01", setting.Version);
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
    public async Task Update_ChangesNameAndSetting_KeepsType()
    {
        var provider = await CreateProviderAsync($"upd-{Guid.NewGuid():N}");

        var newSetting = new OpenAIModelProviderSetting
        {
            Type = ModelProviderType.OpenAI,
            BaseUrl = "https://vip.api.openai.com/v1",
            ApiKey = "sk-new",
            Organization = "org-2",
        };
        await _host.PublishAsync(new UpdateModelProviderCommand(provider.Id, new UpdateModelProviderDto
        {
            Name = "改名提供商",
            Setting = newSetting,
            Mappings = [],
        }));

        var updated = await GetProviderAsync(provider.Id);

        Assert.Equal("改名提供商", updated.Name);
        var setting = Assert.IsType<OpenAIModelProviderSetting>(updated.Setting);
        Assert.Equal("https://vip.api.openai.com/v1", setting.BaseUrl);
        Assert.Equal("org-2", setting.Organization);
    }

    [Fact]
    public async Task Update_TypeChange_Throws()
    {
        var provider = await CreateProviderAsync($"type-{Guid.NewGuid():N}", ModelProviderType.OpenAI);

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(
            new UpdateModelProviderCommand(provider.Id, new UpdateModelProviderDto
            {
                Name = provider.Name,
                Setting = TestDataFactory.CreateProviderSetting(ModelProviderType.Anthropic),
            })));
        Assert.Contains("类型", ex.Message);
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
    public async Task Delete_RemovesProvider()
    {
        var provider = await CreateProviderAsync($"del-{Guid.NewGuid():N}");

        await _host.PublishAsync(new DeleteModelProviderCommand(provider.Id));

        var query = new GetModelProviderQuery(provider.Id);
        await _host.PublishAsync(query);
        Assert.Null(query.Result);
    }

    [Fact]
    public async Task Create_WithMappings_ReturnsThemByScoreDesc_WithModelName()
    {
        var modelA = await CreateModelAsync("甲");
        var modelB = await CreateModelAsync("乙");
        var modelC = await CreateModelAsync("丙");

        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"map-{Guid.NewGuid():N}", mappings:
        [
            TestDataFactory.Mapping(modelA, 10),
            TestDataFactory.Mapping(modelB, 30, "zhipu/glm-5.3"),
            TestDataFactory.Mapping(modelC, 10),
        ]);

        var provider = await GetProviderAsync(providerId);

        // 分数高的排前面；分数只是排序依据，不要求唯一（甲、丙同为 10）
        Assert.Equal([modelB, modelA, modelC], provider.Mappings.Select(x => x.ModelId).ToList());
        Assert.Equal([30, 10, 10], provider.Mappings.Select(x => x.Priority).ToList());

        // 模型显示名称由查询侧回填；未填提供商侧模型名时为空字符串（表示回落模型名称）
        var top = provider.Mappings[0];
        Assert.Contains("模型乙", top.ModelDisplayName);
        Assert.Equal("zhipu/glm-5.3", top.ProviderModelName);
        Assert.Equal(string.Empty, provider.Mappings[1].ProviderModelName);
    }

    [Fact]
    public async Task Create_SameModelTwice_Throws_AndSavesNothing()
    {
        var modelId = await CreateModelAsync("重复");
        var name = $"twice-{Guid.NewGuid():N}";

        // 一个提供方对一个模型只保留一条映射：聚合内部不变量
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => TestDataFactory.CreateModelProviderAsync(_host, name, mappings:
            [
                TestDataFactory.Mapping(modelId, 10),
                TestDataFactory.Mapping(modelId, 20),
            ]));
        Assert.Contains("只能映射一次", ex.Message);

        var list = new GetModelProviderListQuery();
        await _host.PublishAsync(list);
        Assert.DoesNotContain(list.Result!, x => x.Name == name);
    }

    [Fact]
    public async Task Create_WithNonexistentModel_Throws_AndSavesNothing()
    {
        var name = $"missing-{Guid.NewGuid():N}";

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => TestDataFactory.CreateModelProviderAsync(_host, name, mappings: [TestDataFactory.Mapping(999999)]));
        Assert.Contains("模型不存在", ex.Message);

        var list = new GetModelProviderListQuery();
        await _host.PublishAsync(list);
        Assert.DoesNotContain(list.Result!, x => x.Name == name);
    }

    [Fact]
    public async Task SameModelAndScore_AcrossProviders_IsAllowed()
    {
        var modelId = await CreateModelAsync("同分");

        var firstId = await TestDataFactory.CreateModelProviderAsync(_host, $"same-a-{Guid.NewGuid():N}", mappings: [TestDataFactory.Mapping(modelId, 10)]);
        var secondId = await TestDataFactory.CreateModelProviderAsync(_host, $"same-b-{Guid.NewGuid():N}", mappings: [TestDataFactory.Mapping(modelId, 10)]);

        // 同一个模型被两个提供方以相同分数接入：路由时再按分数排序取第一个
        Assert.Single((await GetProviderAsync(firstId)).Mappings);
        Assert.Single((await GetProviderAsync(secondId)).Mappings);
    }

    [Fact]
    public async Task Update_SyncsMappings_UpdatesKeepsAddsRemoves()
    {
        var modelA = await CreateModelAsync("留");
        var modelB = await CreateModelAsync("删");
        var modelC = await CreateModelAsync("增");
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"sync-{Guid.NewGuid():N}", mappings:
        [
            TestDataFactory.Mapping(modelA, 10),
            TestDataFactory.Mapping(modelB, 20),
        ]);

        await _host.PublishAsync(new UpdateModelProviderCommand(providerId, new UpdateModelProviderDto
        {
            Name = "同步后的提供商",
            Setting = TestDataFactory.CreateProviderSetting(),
            Mappings =
            [
                TestDataFactory.Mapping(modelA, 99, "renamed-model"),
                TestDataFactory.Mapping(modelC, 5),
            ],
        }));

        var updated = await GetProviderAsync(providerId);
        Assert.Equal("同步后的提供商", updated.Name);

        // 甲更新、丙新增、乙移除
        Assert.Equal([modelA, modelC], updated.Mappings.Select(x => x.ModelId).ToList());
        Assert.Equal(99, updated.Mappings[0].Priority);
        Assert.Equal("renamed-model", updated.Mappings[0].ProviderModelName);

        // 被移除的映射不再挡住模型删除
        await _host.PublishAsync(new DeleteModelCommand(modelB));
        var deleted = new GetModelQuery(modelB);
        await _host.PublishAsync(deleted);
        Assert.Null(deleted.Result);
    }

    [Fact]
    public async Task Delete_Provider_RemovesItsMappings()
    {
        var modelId = await CreateModelAsync("级联");
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"cascade-{Guid.NewGuid():N}", mappings: [TestDataFactory.Mapping(modelId)]);

        await _host.PublishAsync(new DeleteModelProviderCommand(providerId));

        // 名下映射随聚合一并删除：模型不再被引用，可以直接删
        await _host.PublishAsync(new DeleteModelCommand(modelId));
        var model = new GetModelQuery(modelId);
        await _host.PublishAsync(model);
        Assert.Null(model.Result);
    }

    [Fact]
    public async Task Mappings_AreIsolatedPerTenant()
    {
        var modelId = await CreateModelAsync("隔离");

        long tenant1ProviderId = 0;
        await WithTenantAsync(1, async () =>
        {
            tenant1ProviderId = await TestDataFactory.CreateModelProviderAsync(_host, "租户一提供商", mappings: [TestDataFactory.Mapping(modelId)]);
            var own = await GetProviderAsync(tenant1ProviderId);
            Assert.Single(own.Mappings);
        });

        // 租户 0 的提供商列表里没有别家租户的提供方，自然也看不到它的映射
        var list = new GetModelProviderListQuery();
        await _host.PublishAsync(list);
        Assert.DoesNotContain(list.Result!, x => x.Id == tenant1ProviderId);
    }
}
