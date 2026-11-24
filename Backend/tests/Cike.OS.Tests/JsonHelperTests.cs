using System.Text.Json;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.Json;
using Cike.OS.Domain.Shared.ValueObjects;
using Xunit;

namespace Cike.OS.Tests;

/// <summary>
/// JsonHelper 的默认选项必须容忍多态判别键不在首位：MySQL 的 json 列会按 key 重排对象成员。
/// 键名与 EF 值转换器输出一致（PascalCase）。
/// </summary>
public class JsonHelperTests
{
    [Fact]
    public void Options_AllowOutOfOrderMetadataProperties()
    {
        Assert.True(JsonHelper.Options.AllowOutOfOrderMetadataProperties);
    }

    [Fact]
    public void Deserialize_TypeFirst_RoundTrips()
    {
        var setting = JsonHelper.Deserialize<ToolSetting>("{\"type\":2,\"Name\":\"x\",\"Endpoint\":\"http://a\"}");

        var http = Assert.IsType<HttpMcpToolSetting>(setting!);
        Assert.Equal("http://a", http.Endpoint);
    }

    [Fact]
    public void Deserialize_TypeNotFirst_StillRoundTrips()
    {
        // Name 与 type 等长，按字节序 Name 排在前面
        var setting = JsonHelper.Deserialize<ToolSetting>("{\"Endpoint\":\"http://a\",\"Name\":\"x\",\"type\":2,\"MaxReconnectionAttempts\":3}");

        var http = Assert.IsType<HttpMcpToolSetting>(setting!);
        Assert.Equal("http://a", http.Endpoint);
        Assert.Equal(3, http.MaxReconnectionAttempts);
    }

    [Fact]
    public void Deserialize_StdioWithNestedDictionary_TypeNotFirst()
    {
        var setting = JsonHelper.Deserialize<ToolSetting>(
            "{\"Command\":\"node\",\"EnvironmentVariables\":{\"A\":{\"Type\":1,\"Value\":\"1\"}},\"type\":3,\"Name\":\"n\"}");

        var stdio = Assert.IsType<StdioMcpToolSetting>(setting!);
        Assert.Equal("node", stdio.Command);
        Assert.Equal("1", stdio.EnvironmentVariables!["A"].Value);
    }

    [Fact]
    public void Serialize_Then_Deserialize_RoundTrips()
    {
        // 必须按基类声明：STJ 只在声明类型是基类时才写判别键（EF 转换器的 lambda 参数就是属性类型 ToolSetting）
        ToolSetting original = new SkillToolSetting { Directory = "skills/0/abc" };

        var json = JsonHelper.Serialize(original);
        Assert.Contains("\"type\":1", json);

        var back = Assert.IsType<SkillToolSetting>(JsonHelper.Deserialize<ToolSetting>(json));
        Assert.Equal("skills/0/abc", back.Directory);
    }

    [Fact]
    public void Serialize_WithConcreteStaticType_OmitsDiscriminator()
    {
        // 已知行为：按具体类型序列化不写判别键，这样出来的 JSON 不能再按基类读回——调用方要按基类声明
        SkillToolSetting concrete = new() { Directory = "skills/0/abc" };

        var json = JsonHelper.Serialize(concrete);

        Assert.DoesNotContain("\"type\"", json);
        Assert.Throws<NotSupportedException>(() => JsonHelper.Deserialize<ToolSetting>(json));
    }

    [Fact]
    public void Deserialize_MissingDiscriminator_Throws()
    {
        Assert.Throws<NotSupportedException>(
            () => JsonHelper.Deserialize<ToolSetting>("{\"Name\":\"x\",\"Endpoint\":\"http://a\"}"));
    }

    [Fact]
    public void Deserialize_LegacyStringDiscriminator_IsRejected()
    {
        // 判别值已从字串改为 ToolType 数值；旧行必须迁移，否则读不出来
        Assert.ThrowsAny<JsonException>(
            () => JsonHelper.Deserialize<ToolSetting>("{\"type\":\"httpMcp\",\"Name\":\"x\",\"Endpoint\":\"http://a\"}"));
    }

    [Fact]
    public void DefaultOptions_StillReject_OutOfOrderDiscriminator()
    {
        var json = "{\"Endpoint\":\"http://a\",\"type\":" + (int)ToolType.HttpMcp + "}";

        Assert.ThrowsAny<Exception>(() => JsonSerializer.Deserialize<ToolSetting>(json, (JsonSerializerOptions?)null));
    }
}
