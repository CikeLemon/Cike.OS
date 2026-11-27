using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.ModelProviders.Validators;

/// <summary>
/// 协议专属设置的字段级输入校验。
/// 设置的多态子类型由判别键决定，必须与协议一一对应；两种 OpenAI 协议各有自己的设置子类，互换即不匹配。
/// </summary>
internal static class ModelProviderSettingRules
{
    public static void Validate(ModelProviderProtocol protocol, ModelProviderSetting? setting, Action<string, string> addFailure)
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
    private static bool Matches(ModelProviderProtocol protocol, ModelProviderSetting setting) => (protocol, setting) switch
    {
        (ModelProviderProtocol.OpenAIChatCompletion, OpenAIChatCompletionModelProviderSetting) => true,
        (ModelProviderProtocol.Anthropic, AnthropicModelProviderSetting) => true,
        (ModelProviderProtocol.OpenAIResponse, OpenAIResponseModelProviderSetting) => true,
        _ => false,
    };
}
