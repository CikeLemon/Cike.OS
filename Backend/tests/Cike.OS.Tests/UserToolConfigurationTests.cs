using Cike.Data.Domain.Repositories;
using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;
using Cike.OS.Application.UserToolConfigurations.Commands;
using Cike.OS.Application.UserToolConfigurations.Queries;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.Domain.ValueObjects;
using Cike.OS.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cike.OS.Tests;

public class UserToolConfigurationTests : IClassFixture<CikeOSTestHost>
{
    private readonly CikeOSTestHost _host;

    public UserToolConfigurationTests(CikeOSTestHost host)
    {
        _host = host;

        using var scope = _host.CreateScope();
        var currentUser = scope.ServiceProvider.GetRequiredService<FakeCurrentUser>();
        currentUser.Id = "user-1";
    }

    private async Task<Tool> CreateToolAsync(bool required = true)
    {
        using var scope = _host.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Tool, long>>();

        var tool = new Tool
        {
            Code = $"tool-{Random.Shared.Next(1000, int.MaxValue)}",
            Name = "Test tool",
            Type = ToolType.Mcp,
            ToolPackageId = 1,
            Setting = new HttpMcpToolSetting
            {
                Name = "Test MCP",
                Endpoint = "http://localhost:5000",
            },
            Parameters =
            [
                new ToolParameterDefinition
                {
                    Code = "ak",
                    Name = "Access key",
                    Required = required,
                    Sensitive = true,
                },
            ],
        };

        return await repository.InsertAsync(tool);
    }

    [Fact]
    public async Task Save_Then_Get_ReturnsUserValues()
    {
        var tool = await CreateToolAsync();

        var saveCommand = new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = tool.Id,
            Values = new Dictionary<string, string> { ["ak"] = "user-123" },
        });
        await _host.PublishAsync(saveCommand);

        var query = new GetUserToolConfigurationQuery(tool.Id);
        await _host.PublishAsync(query);

        Assert.NotNull(query.Result);
        Assert.Equal(tool.Id, query.Result!.ToolId);
        Assert.Equal("user-123", query.Result.Values["ak"]);
    }

    [Fact]
    public async Task Save_Twice_UpdatesExistingConfiguration()
    {
        var tool = await CreateToolAsync();

        var first = new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = tool.Id,
            Values = new Dictionary<string, string> { ["ak"] = "user-123" },
        });
        await _host.PublishAsync(first);

        var second = new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = tool.Id,
            Values = new Dictionary<string, string> { ["ak"] = "user-456" },
        });
        await _host.PublishAsync(second);

        var query = new GetUserToolConfigurationQuery(tool.Id);
        await _host.PublishAsync(query);

        Assert.Equal("user-456", query.Result!.Values["ak"]);
    }

    [Fact]
    public async Task Save_MissingRequiredParameter_Throws()
    {
        var tool = await CreateToolAsync(required: true);

        var command = new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = tool.Id,
            Values = new Dictionary<string, string>(),
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => _host.PublishAsync(command));
    }

    [Fact]
    public async Task Delete_RemovesConfiguration()
    {
        var tool = await CreateToolAsync();

        var saveCommand = new SaveUserToolConfigurationCommand(new SaveUserToolConfigurationDto
        {
            ToolId = tool.Id,
            Values = new Dictionary<string, string> { ["ak"] = "user-123" },
        });
        await _host.PublishAsync(saveCommand);

        await _host.PublishAsync(new DeleteUserToolConfigurationCommand(tool.Id));

        var query = new GetUserToolConfigurationQuery(tool.Id);
        await _host.PublishAsync(query);

        Assert.Null(query.Result);
    }
}
