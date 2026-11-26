using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.ModelProviders.Validators;

/// <summary>
/// 提供商设置的字段级输入校验，创建 / 更新共用。
/// 设置的多态子类型必须与 Type 一一对应；类型不可变由 Handler 比对现有值断言。
/// </summary>
internal static class ModelProviderSettingRules
{
    public static void Validate(ModelProviderSetting? setting, Action<string, string> addFailure)
    {
        switch (setting)
        {
            case null:
                addFailure("Setting", "连接设置不能为空。");
                break;
            case OpenAIModelProviderSetting openAi when openAi.Type == ModelProviderType.OpenAI:
                ValidateCommon(openAi, addFailure);
                if (string.IsNullOrWhiteSpace(openAi.Organization))
                {
                    addFailure("Setting.Organization", "OpenAI 组织标识不能为空。");
                }

                break;
            case AnthropicModelProviderSetting anthropic when anthropic.Type == ModelProviderType.Anthropic:
                ValidateCommon(anthropic, addFailure);
                if (string.IsNullOrWhiteSpace(anthropic.Version))
                {
                    addFailure("Setting.Version", "Anthropic API 版本不能为空。");
                }

                break;
            default:
                addFailure("Setting", "提供商类型与设置类型不匹配。");
                break;
        }
    }

    private static void ValidateCommon(ModelProviderSetting setting, Action<string, string> addFailure)
    {
        if (string.IsNullOrWhiteSpace(setting.BaseUrl))
        {
            addFailure("Setting.BaseUrl", "接入地址不能为空。");
        }

        if (string.IsNullOrWhiteSpace(setting.ApiKey))
        {
            addFailure("Setting.ApiKey", "API 密钥不能为空。");
        }
    }
}
