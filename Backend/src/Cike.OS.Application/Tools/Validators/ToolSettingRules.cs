using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using ModelContextProtocol.Client;

namespace Cike.OS.Application.Tools.Validators;

/// <summary>
/// 工具设置的字段级输入校验，创建 / 更新共用。
/// <paramref name="expectedType"/> 为 null（更新）时只校验设置自身字段；与存储形态是否匹配由聚合根断言。
/// </summary>
internal static class ToolSettingRules
{
    public static void Validate(
        ToolSetting? setting,
        List<ToolParameterDefinition>? parameters,
        ToolType? expectedType,
        Action<string, string> addFailure)
    {
        switch (setting)
        {
            case null:
                return;
            case SkillToolSetting skill when Matches(expectedType, ToolType.Skill):
                if (string.IsNullOrWhiteSpace(skill.Directory))
                {
                    addFailure("Setting.Directory", "技能工具必须上传技能包。");
                }

                break;
            case HttpMcpToolSetting http when Matches(expectedType, ToolType.HttpMcp):
                if (string.IsNullOrWhiteSpace(http.Endpoint))
                {
                    addFailure("Setting.Endpoint", "HTTP MCP 端点不能为空。");
                }

                if (!Enum.IsDefined(http.TransportMode))
                {
                    addFailure("Setting.TransportMode", $"传输模式必须是以下之一：{string.Join(" / ", Enum.GetNames<HttpTransportMode>())}。");
                }

                if (http.ConnectionTimeout <= TimeSpan.Zero)
                {
                    addFailure("Setting.ConnectionTimeout", "连接超时必须大于 0。");
                }

                if (http.MaxReconnectionAttempts < 0)
                {
                    addFailure("Setting.MaxReconnectionAttempts", "最大重连次数不能为负数。");
                }

                if (http.DefaultReconnectionInterval <= TimeSpan.Zero)
                {
                    addFailure("Setting.DefaultReconnectionInterval", "重连间隔必须大于 0。");
                }

                ValidateParameterReferences(http.AdditionalHeaders, parameters, addFailure);
                break;
            case StdioMcpToolSetting stdio when Matches(expectedType, ToolType.StdioMcp):
                if (string.IsNullOrWhiteSpace(stdio.Command))
                {
                    addFailure("Setting.Command", "标准 IO MCP 命令不能为空。");
                }

                ValidateParameterReferences(stdio.EnvironmentVariables, parameters, addFailure);
                break;
            default:
                addFailure("Setting", expectedType is null ? "设置类型不合法。" : "工具形态与设置类型不匹配。");
                break;
        }
    }

    /// <summary>参数编码非空且不重复是聚合不变量，这里提前报出以便给出字段级文案；聚合根工厂仍会兜底。</summary>
    public static void ValidateParameters(List<ToolParameterDefinition>? parameters, Action<string, string> addFailure)
    {
        if (parameters == null)
        {
            return;
        }

        if (parameters.Any(x => string.IsNullOrWhiteSpace(x.Code)))
        {
            addFailure("Parameters", "参数定义中的编码不能为空白。");
        }

        if (parameters.Select(x => x.Code).Distinct(StringComparer.Ordinal).Count() != parameters.Count)
        {
            addFailure("Parameters", "参数定义中的编码不能重复。");
        }
    }

    private static bool Matches(ToolType? expectedType, ToolType actual)
    {
        return expectedType is null || expectedType == actual;
    }

    private static void ValidateParameterReferences(
        IDictionary<string, ToolValue>? values,
        List<ToolParameterDefinition>? parameters,
        Action<string, string> addFailure)
    {
        if (values == null || parameters == null)
        {
            return;
        }

        var codes = parameters.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);
        var missing = values
            .Where(x => x.Value.Type == ToolValueType.ParameterReference && !codes.Contains(x.Value.Value))
            .Select(x => x.Key)
            .ToList();

        if (missing.Count > 0)
        {
            addFailure("Setting", $"以下值引用了未定义的参数：{string.Join(", ", missing)}。");
        }
    }
}
