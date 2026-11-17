using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Domain.ValueObjects;

public class ToolValue
{
    public ToolValueType Type { get; set; }

    public string Value { get; set; } = string.Empty;

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
