using Cike.OS.Domain.Shared.Json;

namespace Cike.OS.EntityFrameworkCore.EntityConfigurations;

/// <summary>
/// EF 值转换表达式树里不能写 <c>?? throw</c>（CS8188），非空 JSON 列的反序列化统一走这里。
/// </summary>
internal static class JsonColumn
{
    public static T Required<T>(string json) where T : class
    {
        return JsonHelper.Deserialize<T>(json)
            ?? throw new InvalidOperationException($"{typeof(T).Name} JSON is invalid.");
    }
}
