using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.ModelProviderEndpoints.Validators;

/// <summary>
/// 协议专属设置的字段级输入校验。
/// 设置的多态子类型由判别键决定，必须与协议一一对应；已有接入点提交的协议是否与现有一致，由聚合断言。
/// </summary>
internal static class ModelProviderEndpointSettingRules
{
    public static void Validate(ModelProviderProtocol protocol, ModelProviderEndpointSetting? setting, Action<string, string> addFailure)
    {
        if (setting == null)
        {
            addFailure("Setting", "协议专属设置不能为空。");
            return;
        }

        if (!Matches(protocol, setting))
        {
            addFailure("Setting", "协议与设置类型不匹配。");
        }
    }

    /// <summary>设置是哪个子类，就对应哪种协议。</summary>
    private static bool Matches(ModelProviderProtocol protocol, ModelProviderEndpointSetting setting) => (protocol, setting) switch
    {
        (ModelProviderProtocol.OpenAIChatCompletion, OpenAIChatCompletionModelProviderEndpointSetting) => true,
        (ModelProviderProtocol.Anthropic, AnthropicModelProviderEndpointSetting) => true,
        (ModelProviderProtocol.OpenAIResponse, OpenAIResponseModelProviderEndpointSetting) => true,
        _ => false,
    };
}
