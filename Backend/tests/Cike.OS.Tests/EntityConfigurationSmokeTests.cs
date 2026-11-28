using Cike.Data.Domain.Repositories;
using Cike.MultiTenancy;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
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
    public async Task Agent_AsAggregateRoot_GetsIdTenant_AndSoftDeletesTools()
    {
        // Agent 是聚合根，Agent 工具是它聚合内的子实体：随 Agent 保存时各自拿到 Id、写入租户与外键；加载用通用仓储的一级预加载
        const long tenantId = 7;
        long agentId;
        long removedToolId;
        long keptToolId;

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();
            using var _ = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantId);

            var agent = AgentEntity.Create(
                "Smoke agent",
                null,
                "You are a helpful assistant.",
                new AgentOptions
                {
                    Temperature = 0.7f,
                    MaxOutputTokens = 2048,
                    StopSequences = ["END"],
                    // M.E.AI 的多态对象（$type 判别）与枚举都要能随 json 列 round-trip
                    ToolMode = ChatToolMode.Auto,
                    Reasoning = new ReasoningOptions { Effort = ReasoningEffort.High },
                },
                [
                    AgentTool.Create(AgentToolType.ToolPackage, 11),
                    AgentTool.Create(AgentToolType.Tool, 22),
                ]);
            dbContext.Add(agent);
            await dbContext.SaveChangesAsync();

            // 同一条绑定只能出现一次是聚合内不变量
            Assert.Throws<UserFriendlyException>(() => agent.SyncTools(
            [
                AgentTool.Create(AgentToolType.Tool, 22),
                AgentTool.Create(AgentToolType.Tool, 22),
            ]));
            // 聚合根与它的子实体随保存各自拿到雪花 Id、写入租户与外键
            agentId = agent.Id;
            removedToolId = agent.Tools[0].Id;
            keptToolId = agent.Tools[1].Id;
            Assert.True(agentId > 0);
            Assert.Equal(tenantId, agent.TenantId);
            Assert.All(agent.Tools, x =>
            {
                Assert.True(x.Id > 0);
                Assert.Equal((tenantId, agentId), (x.TenantId, x.AgentId));
            });
        }

        using (var scope = _host.CreateScope())
        {
            using var _ = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantId);

            // 通用仓储的一级预加载：Agent 连同它的工具一次查全；模型选项从 json 列读回
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<AgentEntity, long>>();
            var loaded = Assert.Single(await repository.ToListAsync(
                repository.WithDetails(x => x.Tools).Where(x => x.Id == agentId)));
            Assert.Equal(2, loaded.Tools.Count);
            Assert.All(loaded.Tools, x => Assert.Equal((tenantId, agentId), (x.TenantId, x.AgentId)));
            Assert.Equal(0.7f, loaded.Options.Temperature);
            Assert.Equal("END", Assert.Single(loaded.Options.StopSequences!));
            Assert.IsType<AutoChatToolMode>(loaded.Options.ToolMode);
            Assert.Equal(ReasoningEffort.High, loaded.Options.Reasoning!.Effort);

            // 同步工具只留一条：被移除的条目在保存时转软删
            loaded.SyncTools([AgentTool.Create(AgentToolType.Tool, 22)]);
            await repository.UpdateAsync(loaded);

            // 删 Agent：它和仍在名下的工具一并软删
            await repository.DeleteAsync(loaded);
        }

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();

            Assert.True((await dbContext.Agents.IgnoreQueryFilters().SingleAsync(x => x.Id == agentId)).IsDeleted);

            var tools = await dbContext.AgentTools.IgnoreQueryFilters()
                .Where(x => x.AgentId == agentId)
                .ToListAsync();
            Assert.Equal(2, tools.Count);
            Assert.All(tools, x => Assert.True(x.IsDeleted));
            Assert.Contains(tools, x => x.Id == removedToolId);
            Assert.Contains(tools, x => x.Id == keptToolId);
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
    public async Task ModelProvider_AsAggregateRoot_GetsIdTenant_AndSoftDeletesModels()
    {
        // 模型提供方是聚合根，模型是它聚合内的子实体：随提供方保存时各自拿到 Id、写入租户与外键；加载用通用仓储的一级预加载
        const long tenantId = 7;
        long providerId;
        long removedModelId;
        long keptModelId;

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();
            using var _ = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantId);

            var provider = ModelProvider.Create(
                "Smoke provider",
                ModelProviderProtocol.OpenAIChatCompletion,
                "https://api.openai.com/v1",
                "sk-test",
                new OpenAIChatCompletionModelProviderSetting { Organization = "org-1" },
                [
                    ModelEntity.Create("gpt-4o", "OpenAI flagship model", [1, 1]),
                    ModelEntity.Create("gpt-4o-mini", null, null),
                ]);
            dbContext.Add(provider);
            await dbContext.SaveChangesAsync();

            // 标签引用去重是聚合内不变量
            Assert.Equal([1L], provider.Models[0].TagIds);
            // 聚合根与它的子实体随保存各自拿到雪花 Id、写入租户与外键
            providerId = provider.Id;
            removedModelId = provider.Models[0].Id;
            keptModelId = provider.Models[1].Id;
            Assert.True(providerId > 0);
            Assert.Equal(tenantId, provider.TenantId);
            Assert.All(provider.Models, x =>
            {
                Assert.True(x.Id > 0);
                Assert.Equal((tenantId, providerId), (x.TenantId, x.ModelProviderId));
            });
        }

        using (var scope = _host.CreateScope())
        {
            using var _ = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantId);

            // 通用仓储的一级预加载：提供方连同它的模型一次查全
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<ModelProvider, long>>();
            var loaded = Assert.Single(await repository.ToListAsync(
                repository.WithDetails(x => x.Models).Where(x => x.Id == providerId)));
            Assert.Equal(2, loaded.Models.Count);
            Assert.All(loaded.Models, x => Assert.Equal((tenantId, providerId), (x.TenantId, x.ModelProviderId)));
            var setting = Assert.IsType<OpenAIChatCompletionModelProviderSetting>(loaded.Setting);
            Assert.Equal("org-1", setting.Organization);

            // 同步模型只留一条：被移除的模型在保存时转软删
            loaded.SyncModels([ModelEntity.Create("gpt-4o-mini", null, null)]);
            await repository.UpdateAsync(loaded);

            // 删提供方：它和仍在名下的模型一并软删
            await repository.DeleteAsync(loaded);
        }

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();

            Assert.True((await dbContext.ModelProviders.IgnoreQueryFilters().SingleAsync(x => x.Id == providerId)).IsDeleted);

            var models = await dbContext.Models.IgnoreQueryFilters()
                .Where(x => x.ModelProviderId == providerId)
                .ToListAsync();
            Assert.Equal(2, models.Count);
            Assert.All(models, x => Assert.True(x.IsDeleted));
            Assert.Contains(models, x => x.Id == removedModelId);
            Assert.Contains(models, x => x.Id == keptModelId);
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
