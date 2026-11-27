using Cike.MultiTenancy;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Commands;
using Cike.OS.Application.ModelProviders.Queries;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cike.OS.Tests;

/// <summary>
/// 模型提供方：业务规则从发布 Command / Query 进入。
/// 提供方是一条连接，模型是它聚合内的子实体、随提供方的请求整体同步（以名称为键）；标签是平台级目录，由 Handler 查库校验引用是否存在。
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

    private async Task<long> CreateAsync(CreateModelProviderDto provider)
    {
        var command = new CreateModelProviderCommand(provider);
        await _host.PublishAsync(command);
        return command.Id;
    }

    private Task UpdateAsync(long providerId, UpdateModelProviderDto provider) =>
        TestDataFactory.UpdateModelProviderAsync(_host, providerId, provider);

    private async Task<ModelProviderDto> GetAsync(long providerId) =>
        await TestDataFactory.GetModelProviderAsync(_host, providerId) ?? throw new InvalidOperationException();

    private async Task<List<ModelProviderListDto>> ListAsync()
    {
        var query = new GetModelProviderListQuery();
        await _host.PublishAsync(query);
        return query.Result!;
    }

    private async Task<bool> ExistsByNameAsync(string name) =>
        (await ListAsync()).Any(x => x.Name == name);

    // ───── 新建：连接与模型 ─────

    [Fact]
    public async Task Create_WithConnectionAndModels_Get_ReturnsConnectionAndModelsSortedByName()
    {
        var name = $"full-{Guid.NewGuid():N}";
        var tagId = await TestDataFactory.CreateTagAsync(_host, $"full-{Guid.NewGuid():N}");

        var providerId = await CreateAsync(TestDataFactory.Provider(name, models:
        [
            TestDataFactory.Model("glm-5.3", "旗舰", [tagId]),
            TestDataFactory.Model("qwen-8-max"),
            TestDataFactory.Model("qwen3.8-flash", "轻量"),
        ]));

        var provider = await GetAsync(providerId);
        Assert.Equal(name, provider.Name);
        Assert.Equal(ModelProviderProtocol.OpenAIChatCompletion, provider.Protocol);
        Assert.Equal("https://api.openai.com/v1", provider.BaseUrl);
        Assert.Equal("sk-test", provider.ApiKey);
        Assert.Equal("org-1", Assert.IsType<OpenAIChatCompletionModelProviderSetting>(provider.Setting).Organization);

        // 模型按名称排序；描述与标签原样带回
        Assert.Equal(["glm-5.3", "qwen-8-max", "qwen3.8-flash"], provider.Models.Select(x => x.Name).ToList());
        Assert.Equal("旗舰", provider.Models[0].Description);
        Assert.Equal([tagId], provider.Models[0].TagIds);
        Assert.Equal(string.Empty, provider.Models[1].Description);
        Assert.Empty(provider.Models[1].TagIds);
    }

    [Fact]
    public async Task Create_WithoutModels_Get_ReturnsEmptyModels()
    {
        // 先立起连接、之后再补模型
        var provider = await GetAsync(await CreateAsync(TestDataFactory.Provider($"bare-{Guid.NewGuid():N}")));

        Assert.Empty(provider.Models);
    }

    [Fact]
    public async Task Create_AnthropicProvider_StoresAnthropicSetting()
    {
        var providerId = await CreateAsync(TestDataFactory.Provider(
            $"anthropic-{Guid.NewGuid():N}",
            ModelProviderProtocol.Anthropic,
            baseUrl: "https://api.anthropic.com",
            apiKey: "ak-test"));

        var provider = await GetAsync(providerId);
        Assert.Equal(ModelProviderProtocol.Anthropic, provider.Protocol);
        Assert.Equal("https://api.anthropic.com", provider.BaseUrl);
        Assert.Equal("ak-test", provider.ApiKey);
        Assert.Equal("2023-06-01", Assert.IsType<AnthropicModelProviderSetting>(provider.Setting).Version);
    }

    [Fact]
    public async Task Create_OpenAIResponseProvider_StoresResponseSetting()
    {
        var providerId = await CreateAsync(TestDataFactory.Provider(
            $"response-{Guid.NewGuid():N}", ModelProviderProtocol.OpenAIResponse));

        var provider = await GetAsync(providerId);
        Assert.Equal(ModelProviderProtocol.OpenAIResponse, provider.Protocol);
        Assert.Equal("org-1", Assert.IsType<OpenAIResponseModelProviderSetting>(provider.Setting).Organization);
    }

    [Fact]
    public async Task Create_WithoutProtocolSpecificFields_StoresNull()
    {
        var providerId = await CreateAsync(TestDataFactory.Provider(
            $"nulls-{Guid.NewGuid():N}", setting: new OpenAIChatCompletionModelProviderSetting()));

        var provider = await GetAsync(providerId);
        Assert.Null(Assert.IsType<OpenAIChatCompletionModelProviderSetting>(provider.Setting).Organization);
    }

    [Fact]
    public async Task Create_ModelWithDuplicateTagReferences_StoresDistinctTagIds()
    {
        var first = await TestDataFactory.CreateTagAsync(_host, $"dup-a-{Guid.NewGuid():N}");
        var second = await TestDataFactory.CreateTagAsync(_host, $"dup-b-{Guid.NewGuid():N}");

        var providerId = await CreateAsync(TestDataFactory.Provider(models: [TestDataFactory.Model("glm-5.3", tagIds: [first, first, second])]));

        Assert.Equal([first, second], (await GetAsync(providerId)).Models[0].TagIds);
    }

    // ───── 名称：同租户内唯一 ─────

    [Fact]
    public async Task Create_DuplicateName_SameTenant_Throws_AndSavesNothing()
    {
        var name = $"dup-{Guid.NewGuid():N}";
        await TestDataFactory.CreateModelProviderAsync(_host, name);

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(
            () => CreateAsync(TestDataFactory.Provider(name, models: [TestDataFactory.Model("glm-5.3")])));
        Assert.Contains("已存在", ex.Message);

        // 什么都不保存：列表里只有先建的那一个
        Assert.Single(await ListAsync(), x => x.Name == name);
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
    public async Task Create_SameName_AfterProviderDeleted_Succeeds()
    {
        // 软删过滤生效：删掉后同名可重建（索引非唯一，见 ADR 0001）
        var name = $"recreate-{Guid.NewGuid():N}";
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, name);

        await _host.PublishAsync(new DeleteModelProviderCommand(providerId));
        var recreatedId = await TestDataFactory.CreateModelProviderAsync(_host, name);

        Assert.Equal(name, (await GetAsync(recreatedId)).Name);
    }

    [Fact]
    public async Task Update_DuplicateName_Throws_AndChangesNothing()
    {
        var takenName = $"taken-{Guid.NewGuid():N}";
        await TestDataFactory.CreateModelProviderAsync(_host, takenName);
        var originalName = $"other-{Guid.NewGuid():N}";
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, originalName);

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => UpdateAsync(providerId, TestDataFactory.ProviderUpdate(takenName)));
        Assert.Contains("已存在", ex.Message);

        var provider = await GetAsync(providerId);
        Assert.Equal(originalName, provider.Name);
    }

    [Fact]
    public async Task Update_RenamesProvider_AndKeepsModels()
    {
        var providerId = await CreateAsync(TestDataFactory.Provider(models: [TestDataFactory.Model("glm-5.3")]));

        await UpdateAsync(providerId, TestDataFactory.ProviderUpdate("改名后的提供方", models: [TestDataFactory.Model("glm-5.3")]));

        var provider = await GetAsync(providerId);
        Assert.Equal("改名后的提供方", provider.Name);
        Assert.Equal(["glm-5.3"], provider.Models.Select(x => x.Name).ToList());
    }

    // ───── 协议：可以修改，设置子类型必须跟着一致 ─────

    [Fact]
    public async Task Update_ChangesProtocolAndSetting_KeepsModels()
    {
        var providerId = await CreateAsync(TestDataFactory.Provider(models: [TestDataFactory.Model("glm-5.3")]));

        // 换协议不用删掉提供方重建：连接字段整体替换，名下模型原样保留
        await UpdateAsync(providerId, TestDataFactory.ProviderUpdate(
            protocol: ModelProviderProtocol.Anthropic,
            models: [TestDataFactory.Model("glm-5.3")],
            baseUrl: "https://api.anthropic.com",
            apiKey: "ak-new"));

        var provider = await GetAsync(providerId);
        Assert.Equal(ModelProviderProtocol.Anthropic, provider.Protocol);
        Assert.Equal("https://api.anthropic.com", provider.BaseUrl);
        Assert.Equal("ak-new", provider.ApiKey);
        Assert.Equal("2023-06-01", Assert.IsType<AnthropicModelProviderSetting>(provider.Setting).Version);
        Assert.Equal(["glm-5.3"], provider.Models.Select(x => x.Name).ToList());
    }

    // ───── 模型：随提供方整体同步，以名称为键 ─────

    [Fact]
    public async Task Update_SyncsModels_UpdatesKeepsAddsRemoves()
    {
        var tagId = await TestDataFactory.CreateTagAsync(_host, $"sync-{Guid.NewGuid():N}");
        var providerId = await CreateAsync(TestDataFactory.Provider(models:
        [
            TestDataFactory.Model("keep", "旧描述"),
            TestDataFactory.Model("remove"),
        ]));
        var keptId = (await GetAsync(providerId)).Models[0].Id;

        await UpdateAsync(providerId, TestDataFactory.ProviderUpdate(models:
        [
            // 名称已存在：只更新描述与标签，仍是原来那一条
            TestDataFactory.Model("keep", "新描述", [tagId]),
            TestDataFactory.Model("add"),
        ]));

        var provider = await GetAsync(providerId);
        Assert.Equal(["add", "keep"], provider.Models.Select(x => x.Name).ToList());
        var kept = provider.Models[1];
        Assert.Equal(keptId, kept.Id);
        Assert.Equal("新描述", kept.Description);
        Assert.Equal([tagId], kept.TagIds);
    }

    [Fact]
    public async Task Update_DuplicateModelNames_Throws_AndChangesNothing()
    {
        var providerId = await CreateAsync(TestDataFactory.Provider(models: [TestDataFactory.Model("glm-5.3")]));

        // 同一提供方下模型名称不重复是聚合内部不变量
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => UpdateAsync(providerId, TestDataFactory.ProviderUpdate(
            baseUrl: "https://changed",
            models: [TestDataFactory.Model("glm-5.3"), TestDataFactory.Model("glm-5.3")])));
        Assert.Contains("只能添加一次", ex.Message);

        var provider = await GetAsync(providerId);
        Assert.Equal("https://api.openai.com/v1", provider.BaseUrl);
        Assert.Equal(["glm-5.3"], provider.Models.Select(x => x.Name).ToList());
    }

    [Fact]
    public async Task Create_DuplicateModelNames_Throws_AndSavesNothing()
    {
        var name = $"twice-{Guid.NewGuid():N}";

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateAsync(TestDataFactory.Provider(name, models:
            [TestDataFactory.Model("glm-5.3"), TestDataFactory.Model("glm-5.3")])));
        Assert.Contains("只能添加一次", ex.Message);

        Assert.False(await ExistsByNameAsync(name));
    }

    [Fact]
    public async Task SameModelName_InTwoProviders_Succeeds()
    {
        // 智谱的 OpenAI 兼容与 Anthropic 兼容各建一个提供方：两个提供方下的同名模型互不相干
        var openAiId = await CreateAsync(TestDataFactory.Provider($"openai-{Guid.NewGuid():N}", models: [TestDataFactory.Model("glm-5.3")]));
        var anthropicId = await CreateAsync(TestDataFactory.Provider(
            $"anthropic-{Guid.NewGuid():N}", ModelProviderProtocol.Anthropic, models: [TestDataFactory.Model("glm-5.3")]));

        Assert.Equal(["glm-5.3"], (await GetAsync(openAiId)).Models.Select(x => x.Name).ToList());
        Assert.Equal(["glm-5.3"], (await GetAsync(anthropicId)).Models.Select(x => x.Name).ToList());
    }

    [Fact]
    public async Task Update_WithoutModels_RemovesAll_KeepsProvider()
    {
        var originalName = $"empty-{Guid.NewGuid():N}";
        var providerId = await CreateAsync(TestDataFactory.Provider(originalName, models: [TestDataFactory.Model("glm-5.3")]));

        // 不带模型提交：名下模型全部移除，连接信息按请求体更新
        await UpdateAsync(providerId, TestDataFactory.ProviderUpdate(originalName));

        var provider = await GetAsync(providerId);
        Assert.Empty(provider.Models);
        Assert.Equal(originalName, provider.Name);
    }

    // ───── 标签：引用必须存在 ─────

    [Fact]
    public async Task Create_WithNonexistentTag_Throws_AndSavesNothing()
    {
        var name = $"tag-{Guid.NewGuid():N}";
        var provider = TestDataFactory.Provider(name, models:
        [
            TestDataFactory.Model("glm-5.3"),
            TestDataFactory.Model("qwen", tagIds: [999999]),
        ]);

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => CreateAsync(provider));
        Assert.Contains("标签不存在", ex.Message);

        Assert.False(await ExistsByNameAsync(name));
    }

    [Fact]
    public async Task Update_WithNonexistentTag_Throws_AndChangesNothing()
    {
        var tagId = await TestDataFactory.CreateTagAsync(_host, $"upd-{Guid.NewGuid():N}");
        var providerId = await CreateAsync(TestDataFactory.Provider(models: [TestDataFactory.Model("glm-5.3", tagIds: [tagId])]));

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => UpdateAsync(providerId, TestDataFactory.ProviderUpdate(
            models: [TestDataFactory.Model("glm-5.3", tagIds: [tagId]), TestDataFactory.Model("qwen", tagIds: [999999])])));
        Assert.Contains("标签不存在", ex.Message);

        Assert.Equal([tagId], (await GetAsync(providerId)).Models[0].TagIds);
    }

    // ───── 删除 / 隔离 / 列表 ─────

    [Fact]
    public async Task Delete_RemovesProvider()
    {
        var providerId = await TestDataFactory.CreateModelProviderAsync(_host, $"del-{Guid.NewGuid():N}");

        await _host.PublishAsync(new DeleteModelProviderCommand(providerId));

        Assert.Null(await TestDataFactory.GetModelProviderAsync(_host, providerId));
    }

    [Fact]
    public async Task Delete_NonexistentProvider_Throws()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(new DeleteModelProviderCommand(999999)));
        Assert.Contains("模型提供方不存在", ex.Message);
    }

    [Fact]
    public async Task Update_NonexistentProvider_Throws()
    {
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => UpdateAsync(999999, TestDataFactory.ProviderUpdate("幽灵")));
        Assert.Contains("模型提供方不存在", ex.Message);
    }

    [Fact]
    public async Task Provider_OfAnotherTenant_IsInvisible_AndCannotBeTakenOver()
    {
        long tenant1ProviderId = 0;
        await WithTenantAsync(1, async () =>
        {
            tenant1ProviderId = await TestDataFactory.CreateModelProviderAsync(_host, $"租户一提供商-{Guid.NewGuid():N}");
        });

        // 租户 0 看不到租户 1 的提供方：读取为空，修改 / 删除按「模型提供方不存在」被拒
        Assert.Null(await TestDataFactory.GetModelProviderAsync(_host, tenant1ProviderId));
        Assert.DoesNotContain(await ListAsync(), x => x.Id == tenant1ProviderId);

        var updateEx = await Assert.ThrowsAsync<UserFriendlyException>(() => UpdateAsync(tenant1ProviderId, TestDataFactory.ProviderUpdate("抢占")));
        Assert.Contains("模型提供方不存在", updateEx.Message);

        var deleteEx = await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(new DeleteModelProviderCommand(tenant1ProviderId)));
        Assert.Contains("模型提供方不存在", deleteEx.Message);

        await WithTenantAsync(1, async () =>
        {
            Assert.NotNull(await TestDataFactory.GetModelProviderAsync(_host, tenant1ProviderId));
        });
    }

    [Fact]
    public async Task List_OrdersByName()
    {
        await TestDataFactory.CreateModelProviderAsync(_host, $"b-{Guid.NewGuid():N}");
        await TestDataFactory.CreateModelProviderAsync(_host, $"a-{Guid.NewGuid():N}");

        var names = (await ListAsync()).Select(x => x.Name).ToList();
        Assert.True(names.Count >= 2);
        Assert.Equal(names.OrderBy(x => x, StringComparer.Ordinal), names);
    }

    [Fact]
    public async Task List_RowHasProtocolAndSortedModelNames()
    {
        var name = $"summary-{Guid.NewGuid():N}";
        var providerId = await CreateAsync(TestDataFactory.Provider(
            name, ModelProviderProtocol.Anthropic, models: [TestDataFactory.Model("qwen"), TestDataFactory.Model("glm-5.3")]));

        var row = (await ListAsync()).Single(x => x.Id == providerId);
        Assert.Equal(ModelProviderProtocol.Anthropic, row.Protocol);
        Assert.Equal(["glm-5.3", "qwen"], row.ModelNames);
        Assert.True(row.Id > 0);
    }
}
