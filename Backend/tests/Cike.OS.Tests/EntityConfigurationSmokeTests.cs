using Cike.MultiTenancy;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Repositories;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.Domain.ValueObjects;
using Cike.OS.EntityFrameworkCore;
using Cike.OS.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Xunit;

namespace Cike.OS.Tests;

public class EntityConfigurationSmokeTests : IClassFixture<CikeOSTestHost>
{
    private readonly CikeOSTestHost _host;

    public EntityConfigurationSmokeTests(CikeOSTestHost host)
    {
        _host = host;
    }

    [Fact]
    public void Tool_DerivedQueryProperties_AreNotMappedByConvention()
    {
        // IsSkill / SkillPackageDirectory 是聚合上的查询属性（只读、无后备字段），EF 按约定不映射；
        // 一旦有人给它们加 setter 或引入了批量映射约定，这里会先报出来，避免悄悄多出两列
        using var scope = _host.CreateScope();
        var entityType = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>().Model.FindEntityType(typeof(Tool))!;

        Assert.Null(entityType.FindProperty(nameof(Tool.IsSkill)));
        Assert.Null(entityType.FindProperty(nameof(Tool.SkillPackageDirectory)));
        Assert.NotNull(entityType.FindProperty(nameof(Tool.Type)));
    }

    [Fact]
    public async Task Agent_Domain_RoundTrip()
    {
        long agentId;
        long packageId;

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();

            var agent = new AgentEntity
            {
                Name = "Smoke agent",
                Description = "Agent round trip",
                Instructions = "You are a helpful assistant.",
                Options = new AgentOptions
                {
                    Temperature = 0.7f,
                    MaxOutputTokens = 2048,
                    StopSequences = ["END"],
                },
            };
            dbContext.Add(agent);
            await dbContext.SaveChangesAsync();
            agentId = agent.Id;

            var package = new AgentToolPackage
            {
                AgentId = agentId,
                ToolPackageId = 1,
                IsFull = true,
                AgentTools =
                [
                    new AgentTool { AgentToolPackageId = 0, ToolId = 1 },
                ],
            };
            dbContext.Add(package);
            await dbContext.SaveChangesAsync();
            packageId = package.Id;
        }

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();

            var agent = await dbContext.Agents.AsNoTracking().SingleAsync(x => x.Id == agentId);
            Assert.Equal("Smoke agent", agent.Name);
            Assert.Equal(0.7f, agent.Options.Temperature);
            Assert.Equal(2048, agent.Options.MaxOutputTokens);
            Assert.Equal("END", Assert.Single(agent.Options.StopSequences!));

            var package = await dbContext.AgentToolPackages.AsNoTracking()
                .Include(x => x.AgentTools)
                .SingleAsync(x => x.Id == packageId);
            Assert.True(package.IsFull);
            var child = Assert.Single(package.AgentTools);
            Assert.Equal(packageId, child.AgentToolPackageId);
            Assert.Equal(1, child.ToolId);
        }
    }

    [Fact]
    public async Task AgentToolPackage_Delete_CascadesToChildren()
    {
        long packageId;

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();

            var package = new AgentToolPackage
            {
                AgentId = 1,
                ToolPackageId = 1,
                AgentTools =
                [
                    new AgentTool { AgentToolPackageId = 0, ToolId = 1 },
                ],
            };
            dbContext.Add(package);
            await dbContext.SaveChangesAsync();
            packageId = package.Id;
        }

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();
            // 软删聚合根不会触发数据库级联（DELETE 被改写为软删 UPDATE），
            // 子项必须先加载跟踪，客户端级联才会把子项一并标记删除。
            var package = await dbContext.AgentToolPackages
                .Include(x => x.AgentTools)
                .SingleAsync(x => x.Id == packageId);
            dbContext.Remove(package);
            await dbContext.SaveChangesAsync();
        }

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();

            Assert.False(await dbContext.AgentToolPackages.IgnoreQueryFilters().AnyAsync(x => x.Id == packageId && !x.IsDeleted));

            var child = await dbContext.AgentTools.IgnoreQueryFilters().SingleAsync(x => x.AgentToolPackageId == packageId);
            Assert.True(child.IsDeleted);
        }
    }

    [Fact]
    public async Task Conversation_Domain_RoundTrip()
    {
        long conversationId;
        long messageId;
        long eventId;

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();

            var conversation = new Conversation
            {
                Title = "Smoke conversation",
                Type = ConversationType.Agent,
                AgentId = 1,
            };
            dbContext.Add(conversation);
            await dbContext.SaveChangesAsync();
            conversationId = conversation.Id;

            var member = new ConversationMember
            {
                ConversationId = conversationId,
                MemberId = 42,
                Role = ConversationMemberRole.Admin,
            };
            dbContext.Add(member);

            var file = new ConversationFile
            {
                ConversationId = conversationId,
                Sender = 42,
                Name = "report.pdf",
                Size = 1024,
                MimeType = "application/pdf",
                StoragePath = "/files/report.pdf",
            };
            dbContext.Add(file);

            var message = new ConversationMessage
            {
                ConversationId = conversationId,
                SenderRole = new ChatRole("user"),
                SenderId = 42,
            };
            dbContext.Add(message);
            await dbContext.SaveChangesAsync();
            messageId = message.Id;

            var messageEvent = new ConversationMessageEvent
            {
                ConversationId = conversationId,
                ConversationMessageId = messageId,
                ActionId = "tool_call",
                ActionName = "Search",
                Type = "action",
                Content = "event content round trip",
                Duration = 120,
            };
            dbContext.Add(messageEvent);
            await dbContext.SaveChangesAsync();
            eventId = messageEvent.Id;
        }

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();

            var member = await dbContext.ConversationMembers.AsNoTracking().SingleAsync(x => x.ConversationId == conversationId);
            Assert.Equal(42, member.MemberId);
            Assert.Equal(ConversationMemberRole.Admin, member.Role);

            var message = await dbContext.ConversationMessages.AsNoTracking().SingleAsync(x => x.Id == messageId);
            Assert.Equal(conversationId, message.ConversationId);
            Assert.Equal("user", message.SenderRole.Value);
            Assert.Equal(42, message.SenderId);

            var messageEvent = await dbContext.ConversationMessageEvents.AsNoTracking().SingleAsync(x => x.Id == eventId);
            Assert.Equal(conversationId, messageEvent.ConversationId);
            Assert.Equal(messageId, messageEvent.ConversationMessageId);
            Assert.Equal("tool_call", messageEvent.ActionId);
            Assert.Equal("action", messageEvent.Type);
            Assert.Equal(120, messageEvent.Duration);
        }
    }

    [Fact]
    public async Task ModelProvider_ThreeLevelAggregate_GrandchildrenGetIdTenantAndSoftDelete()
    {
        // 提供方 → 接入点 → 映射是仓库里第一个三层聚合：孙实体的 Id / 租户 / 软删行为没有文档依据，在这里实测
        const long tenantId = 7;
        long providerId;
        long removedEndpointId;
        long removedMappingId;
        long keptEndpointId;
        long keptMappingId;
        long modelId;

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();
            using var _ = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantId);

            var model = ModelEntity.Create("gpt-4o", "GPT-4o", "OpenAI flagship model", []);
            dbContext.Add(model);
            await dbContext.SaveChangesAsync();
            modelId = model.Id;

            // 接入点与映射是聚合内子实体：只能经聚合根加入，与聚合根一起落库
            var provider = ModelProvider.Create("Smoke provider");
            provider.SyncEndpoints([
                new ModelProvider.EndpointInput(
                    0,
                    ModelProviderProtocol.OpenAI,
                    "官方",
                    "https://api.openai.com/v1",
                    "sk-test",
                    new OpenAIModelProviderEndpointSetting { Organization = "org-1" },
                    [(modelId, 10, "gpt-4o-2024-11-20")]),
            ]);
            dbContext.Add(provider);
            await dbContext.SaveChangesAsync();

            providerId = provider.Id;
            removedEndpointId = provider.Endpoints[0].Id;
            removedMappingId = provider.Endpoints[0].Mappings[0].Id;
        }

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();
            using var _ = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantId);

            // 自定义仓储：两级 Include 一次把接入点与映射查全（通用仓储的 WithDetails 只能预加载一级）
            var repository = scope.ServiceProvider.GetRequiredService<IModelProviderRepository>();
            var provider = await repository.FindWithEndpointsAsync(providerId);
            Assert.NotNull(provider);

            var removedEndpoint = Assert.Single(provider.Endpoints);
            var removedMapping = Assert.Single(removedEndpoint.Mappings);
            Assert.Equal(removedEndpointId, removedEndpoint.Id);
            Assert.Equal(removedMappingId, removedMapping.Id);

            // 孙实体随聚合保存时自动生成雪花 Id、写入租户，外键由 EF 关系修正回填
            Assert.True(removedMapping.Id > 0);
            Assert.Equal(tenantId, removedEndpoint.TenantId);
            Assert.Equal(tenantId, removedMapping.TenantId);
            Assert.Equal(providerId, removedEndpoint.ModelProviderId);
            Assert.Equal(removedEndpointId, removedMapping.ModelProviderEndpointId);
            var setting = Assert.IsType<OpenAIModelProviderEndpointSetting>(removedEndpoint.Setting);
            Assert.Equal("org-1", setting.Organization);

            // 既有聚合根同步子实体：已有的保留、Id 为 0 的新增，同样自动拿到 Id
            provider.SyncEndpoints(
            [
                new ModelProvider.EndpointInput(
                    removedEndpointId,
                    ModelProviderProtocol.OpenAI,
                    "官方",
                    "https://api.openai.com/v1",
                    "sk-test",
                    new OpenAIModelProviderEndpointSetting { Organization = "org-1" },
                    [(modelId, 10, "gpt-4o-2024-11-20")]),
                new ModelProvider.EndpointInput(
                    0,
                    ModelProviderProtocol.Anthropic,
                    "官方 Anthropic",
                    "https://api.anthropic.com",
                    "ak-test",
                    new AnthropicModelProviderEndpointSetting(),
                    [(modelId, 5, null)]),
            ]);
            await dbContext.SaveChangesAsync();
            var keptEndpoint = provider.Endpoints.Single(x => x.Id != removedEndpointId);
            keptEndpointId = keptEndpoint.Id;
            keptMappingId = keptEndpoint.Mappings[0].Id;
            Assert.True(keptEndpointId > 0);
            Assert.True(keptMappingId > 0);

            // 映射随接入点整体同步：未提交的被移除；从集合移除的子实体在保存时转软删
            provider.SyncEndpoints(
            [
                new ModelProvider.EndpointInput(
                    removedEndpointId,
                    ModelProviderProtocol.OpenAI,
                    "官方",
                    "https://vip.api.openai.com/v1",
                    "sk-new",
                    new OpenAIModelProviderEndpointSetting(),
                    []),
                new ModelProvider.EndpointInput(
                    keptEndpointId,
                    ModelProviderProtocol.Anthropic,
                    "官方 Anthropic",
                    "https://api.anthropic.com",
                    "ak-test",
                    new AnthropicModelProviderEndpointSetting(),
                    [(modelId, 5, null)]),
            ]);
            await dbContext.SaveChangesAsync();

            // 未提交的接入点被移除：接入点与其映射一并软删
            provider.SyncEndpoints(
            [
                new ModelProvider.EndpointInput(
                    keptEndpointId,
                    ModelProviderProtocol.Anthropic,
                    "官方 Anthropic",
                    "https://api.anthropic.com",
                    "ak-test",
                    new AnthropicModelProviderEndpointSetting(),
                    [(modelId, 5, null)]),
            ]);
            await dbContext.SaveChangesAsync();
        }

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();
            using var _ = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantId);

            var repository = scope.ServiceProvider.GetRequiredService<IModelProviderRepository>();
            var provider = await repository.FindWithEndpointsAsync(providerId);
            Assert.Equal("官方 Anthropic", Assert.Single(provider!.Endpoints).Name);

            // 删聚合根：先连同两层子集合加载跟踪，接入点与映射才会跟着软删（软删不触发数据库级联）
            dbContext.Remove(provider);
            await dbContext.SaveChangesAsync();
        }

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();

            Assert.False(await dbContext.ModelProviders.IgnoreQueryFilters().AnyAsync(x => x.Id == providerId && !x.IsDeleted));

            var endpoints = await dbContext.ModelProviderEndpoints.IgnoreQueryFilters()
                .Where(x => x.ModelProviderId == providerId)
                .ToListAsync();
            Assert.Equal(2, endpoints.Count);
            Assert.All(endpoints, x => Assert.True(x.IsDeleted));

            var mappings = await dbContext.ModelProviderMappings.IgnoreQueryFilters()
                .Where(x => x.ModelId == modelId && x.TenantId == tenantId)
                .ToListAsync();
            Assert.Equal(2, mappings.Count);
            Assert.All(mappings, x => Assert.True(x.IsDeleted));
            Assert.Contains(mappings, x => x.Id == removedMappingId);
            Assert.Contains(mappings, x => x.Id == keptMappingId);
        }
    }

    [Fact]
    public async Task Tool_Domain_RoundTrip()
    {
        long toolId;

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();

            var category = ToolPackageCategory.Create("生产力");
            dbContext.Add(category);
            await dbContext.SaveChangesAsync();

            var package = ToolPackage.Create(
                "smoke-package", "Smoke package", "Tool package round trip", "icon", "# readme", category.Id, ToolPackageStatus.Published);
            dbContext.Add(package);
            await dbContext.SaveChangesAsync();

            var tool = Tool.Create(
                "smoke-tool",
                "Smoke tool",
                "Tool round trip",
                ToolType.HttpMcp,
                package.Id,
                [
                    new ToolParameterDefinition
                    {
                        Code = "ak",
                        Name = "Access key",
                        Required = true,
                        Sensitive = true,
                    },
                ],
                new HttpMcpToolSetting
                {
                    Name = "Smoke MCP",
                    Endpoint = "http://localhost:5000/mcp",
                    ConnectionTimeout = TimeSpan.FromSeconds(15),
                    MaxReconnectionAttempts = 3,
                });
            dbContext.Add(tool);
            await dbContext.SaveChangesAsync();
            toolId = tool.Id;
        }

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();

            var tool = await dbContext.Tools.AsNoTracking().SingleAsync(x => x.Id == toolId);
            Assert.Equal(ToolType.HttpMcp, tool.Type);

            var setting = Assert.IsType<HttpMcpToolSetting>(tool.Setting);
            Assert.Equal("Smoke MCP", setting.Name);
            Assert.Equal("http://localhost:5000/mcp", setting.Endpoint);
            Assert.Equal(TimeSpan.FromSeconds(15), setting.ConnectionTimeout);
            Assert.Equal(3, setting.MaxReconnectionAttempts);

            var parameter = Assert.Single(tool.Parameters);
            Assert.Equal("ak", parameter.Code);
            Assert.True(parameter.Required);
            Assert.True(parameter.Sensitive);
        }
    }
}
