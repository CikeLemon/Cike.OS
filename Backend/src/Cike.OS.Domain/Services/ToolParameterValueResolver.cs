using Cike.Core.DependencyInjection;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.ValueObjects;

namespace Cike.OS.Domain.Services;

public class ToolParameterValueResolver : ISingletonDependency
{
    public IReadOnlyDictionary<string, string> ResolveUserValues(
        IEnumerable<ToolParameterDefinition> parameters,
        IReadOnlyDictionary<string, string> userValues)
    {
        var result = new Dictionary<string, string>();
        var declarations = parameters as ToolParameterDefinition[] ?? parameters.ToArray();

        foreach (var parameter in declarations)
        {
            if (string.IsNullOrWhiteSpace(parameter.Code))
            {
                throw new UserFriendlyException("Tool parameter code cannot be empty.");
            }

            if (userValues.TryGetValue(parameter.Code, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                result[parameter.Code] = value;
            }
            else if (parameter.Required)
            {
                throw new UserFriendlyException($"Tool parameter '{parameter.Code}' is required.");
            }
        }

        return result;
    }

    public IReadOnlyDictionary<string, string> ResolveValueSlots(
        IReadOnlyDictionary<string, ToolValue> valueSlots,
        IReadOnlyDictionary<string, string> userValues,
        IEnumerable<ToolParameterDefinition> parameters)
    {
        var result = new Dictionary<string, string>();

        foreach (var (slotName, toolValue) in valueSlots)
        {
            result[slotName] = Resolve(toolValue, userValues, parameters);
        }

        return result;
    }

    public string Resolve(
        ToolValue toolValue,
        IReadOnlyDictionary<string, string> userValues,
        IEnumerable<ToolParameterDefinition> parameters)
    {
        if (toolValue.Type == ToolValueType.Literal)
        {
            return toolValue.Value;
        }

        if (toolValue.Type == ToolValueType.ParameterReference)
        {
            if (!parameters.Any(p => p.Code == toolValue.Value))
            {
                throw new UserFriendlyException($"Tool parameter '{toolValue.Value}' is not declared.");
            }

            if (!userValues.TryGetValue(toolValue.Value, out var value) ||
                string.IsNullOrWhiteSpace(value))
            {
                throw new UserFriendlyException($"Tool parameter '{toolValue.Value}' has no value.");
            }

            return value;
        }

        throw new UserFriendlyException("A tool value must be either a literal or a parameter reference.");
    }
}
