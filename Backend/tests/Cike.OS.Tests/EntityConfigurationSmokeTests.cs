using Cike.OS.Domain.Entities;
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
    public async Task Model_Domain_RoundTrip()
    {
        long providerId;
        long modelId;

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();

            var provider = new ModelProvider
            {
                Name = "Smoke provider",
                Setting = new OpenAIModelProviderSetting
                {
                    Type = ModelProviderType.OpenAI,
                    BaseUrl = "https://api.openai.com/v1",
                    ApiKey = "sk-test",
                    Organization = "org-1",
                },
            };
            dbContext.Add(provider);
            await dbContext.SaveChangesAsync();
            providerId = provider.Id;

            var tag = new Tag { Key = "vision", Name = "视觉" };
            dbContext.Add(tag);
            await dbContext.SaveChangesAsync();

            var model = new ModelEntity
            {
                ModelProviderId = providerId,
                Name = "gpt-4o",
                DisplayName = "GPT-4o",
                TagIds = [tag.Id],
            };
            dbContext.Add(model);
            await dbContext.SaveChangesAsync();
            modelId = model.Id;
        }

        using (var scope = _host.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CikeOSDbContext>();

            var provider = await dbContext.ModelProviders.AsNoTracking().SingleAsync(x => x.Id == providerId);
            var setting = Assert.IsType<OpenAIModelProviderSetting>(provider.Setting);
            Assert.Equal(ModelProviderType.OpenAI, setting.Type);
            Assert.Equal("https://api.openai.com/v1", setting.BaseUrl);
            Assert.Equal("sk-test", setting.ApiKey);
            Assert.Equal("org-1", setting.Organization);

            var model = await dbContext.Models.AsNoTracking().SingleAsync(x => x.Id == modelId);
            Assert.Equal("GPT-4o", model.DisplayName);
            Assert.Single(model.TagIds);
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
