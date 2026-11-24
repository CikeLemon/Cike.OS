using System.Text.Json;

namespace Cike.OS.Domain.Shared.Json;

/// <summary>
/// 项目统一的 System.Text.Json 入口：手写序列化 / 反序列化（EF 值转换等）一律走这里，不要各自传 <c>(JsonSerializerOptions?)null</c>。
///
/// 默认值：
/// - <see cref="JsonSerializerOptions.AllowOutOfOrderMetadataProperties"/> = true：
///   MySQL 的 json 列会按 key 重排对象成员，多态判别键 <c>type</c> 不保证在首位，而 STJ 默认要求它在对象开头。
/// - 其余保持 STJ 默认（PascalCase、不忽略 null）：已落库的 JSON 都是按这个形状写的，改任何一项都等于数据迁移。
/// </summary>
public static class JsonHelper
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        AllowOutOfOrderMetadataProperties = true,
    };

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }
}
