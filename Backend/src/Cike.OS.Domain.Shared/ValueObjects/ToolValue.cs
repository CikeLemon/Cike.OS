namespace Cike.OS.Domain.Shared.ValueObjects;

public class ToolValue
{
    public ToolValueType Type { get; set; }

    public string Value { get; set; } = string.Empty;

    /// <summary>派生属性，不参与序列化（值对象直接作为 API 契约，响应里不该多出只读字段）。</summary>
    [JsonIgnore]
    public bool IsParameterReference => Type == ToolValueType.ParameterReference;

    public static ToolValue FromLiteral(string literal)
    {
        return new ToolValue { Type = ToolValueType.Literal, Value = literal };
    }

    public static ToolValue FromParameter(string parameterCode)
    {
        return new ToolValue { Type = ToolValueType.ParameterReference, Value = parameterCode };
    }
}
