namespace Cike.OS.Domain.Shared.ValueObjects;

/// <summary>
/// 模型提供方的协议专属设置（多态值对象）。位于 Domain.Shared，直接作为 API 契约与持久化形状：
/// 判别键为 <c>type</c>，取值与 <see cref="Enums.ModelProviderProtocol"/> 一致。
/// 接入地址与密钥是每个提供方都有的字段，放在提供方实体上；这里只放某种协议独有的字段，均为可选。
/// 基类上不放「当前是哪个协议」的投影属性：它是哪个子类，就已经说明了协议。
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(OpenAIChatCompletionModelProviderSetting), (int)Enums.ModelProviderProtocol.OpenAIChatCompletion)]
[JsonDerivedType(typeof(AnthropicModelProviderSetting), (int)Enums.ModelProviderProtocol.Anthropic)]
[JsonDerivedType(typeof(OpenAIResponseModelProviderSetting), (int)Enums.ModelProviderProtocol.OpenAIResponse)]
public abstract class ModelProviderSetting
{
}

/// <summary>OpenAI Chat Completion 协议的专属设置。</summary>
public class OpenAIChatCompletionModelProviderSetting : ModelProviderSetting
{
    /// <summary>OpenAI 组织标识（请求头 OpenAI-Organization），可选。</summary>
    public string? Organization { get; set; }
}

/// <summary>OpenAI Response 协议的专属设置。</summary>
public class OpenAIResponseModelProviderSetting : ModelProviderSetting
{
    /// <summary>OpenAI 组织标识（请求头 OpenAI-Organization），可选。</summary>
    public string? Organization { get; set; }
}

public class AnthropicModelProviderSetting : ModelProviderSetting
{
    /// <summary>Anthropic API 版本（请求头 anthropic-version），可选。</summary>
    public string? Version { get; set; }
}
