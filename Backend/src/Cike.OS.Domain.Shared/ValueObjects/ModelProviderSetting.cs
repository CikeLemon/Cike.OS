namespace Cike.OS.Domain.Shared.ValueObjects;

/// <summary>
/// 模型提供方连接设置（多态值对象）。位于 Domain.Shared，直接作为 API 契约与持久化形状：
/// 判别键为 <c>type</c>，取值与 <see cref="ModelProviderType"/> 一致。
/// <see cref="Type"/> 只是运行时子类型的冗余投影，不进 JSON（Web 的 camelCase 会与判别键 <c>type</c> 撞名），由子类构造函数赋值。
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(OpenAIModelProviderSetting), (int)ModelProviderType.OpenAI)]
[JsonDerivedType(typeof(AnthropicModelProviderSetting), (int)ModelProviderType.Anthropic)]
public abstract class ModelProviderSetting
{
    [JsonIgnore]
    public ModelProviderType Type { get; set; }

    public string BaseUrl { get; set; } = null!;

    public string ApiKey { get; set; } = null!;
}

public class OpenAIModelProviderSetting : ModelProviderSetting
{
    public OpenAIModelProviderSetting()
    {
        Type = ModelProviderType.OpenAI;
    }

    public string Organization { get; set; } = null!;
}

public class AnthropicModelProviderSetting : ModelProviderSetting
{
    public AnthropicModelProviderSetting()
    {
        Type = ModelProviderType.Anthropic;
    }

    public string Version { get; set; } = null!;
}
