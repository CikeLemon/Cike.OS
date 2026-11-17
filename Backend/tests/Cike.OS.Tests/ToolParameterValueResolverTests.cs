using Cike.OS.Domain.Services;
using Cike.OS.Domain.ValueObjects;
using Xunit;

namespace Cike.OS.Tests;

public class ToolParameterValueResolverTests
{
    private readonly ToolParameterValueResolver _resolver = new();

    [Fact]
    public void Resolve_LiteralValue_ReturnsLiteral()
    {
        var parameters = new List<ToolParameterDefinition>
        {
            new() { Code = "ak", Required = true },
        };

        var result = _resolver.Resolve(
            ToolValue.FromLiteral("fixed-value"),
            new Dictionary<string, string>(),
            parameters);

        Assert.Equal("fixed-value", result);
    }

    [Fact]
    public void Resolve_ParameterReference_ReturnsUserValue()
    {
        var parameters = new List<ToolParameterDefinition>
        {
            new() { Code = "ak", Required = true },
        };

        var result = _resolver.Resolve(
            ToolValue.FromParameter("ak"),
            new Dictionary<string, string> { ["ak"] = "user-123" },
            parameters);

        Assert.Equal("user-123", result);
    }

    [Fact]
    public void Resolve_MissingRequiredParameter_Throws()
    {
        var parameters = new List<ToolParameterDefinition>
        {
            new() { Code = "ak", Required = true },
        };

        Assert.Throws<UserFriendlyException>(() =>
            _resolver.ResolveUserValues(parameters, new Dictionary<string, string>()));
    }

    [Fact]
    public void Resolve_UndeclaredParameterReference_Throws()
    {
        var parameters = new List<ToolParameterDefinition>();

        Assert.Throws<UserFriendlyException>(() =>
            _resolver.Resolve(
                ToolValue.FromParameter("ak"),
                new Dictionary<string, string> { ["ak"] = "user-123" },
                parameters));
    }

    [Fact]
    public void ResolveValueSlots_ResolvesMultipleSlots()
    {
        var parameters = new List<ToolParameterDefinition>
        {
            new() { Code = "ak", Required = true },
        };

        var slots = new Dictionary<string, ToolValue>
        {
            ["X-API-Key"] = ToolValue.FromParameter("ak"),
            ["X-Static"] = ToolValue.FromLiteral("static"),
        };

        var result = _resolver.ResolveValueSlots(
            slots,
            new Dictionary<string, string> { ["ak"] = "user-123" },
            parameters);

        Assert.Equal("user-123", result["X-API-Key"]);
        Assert.Equal("static", result["X-Static"]);
    }
}
