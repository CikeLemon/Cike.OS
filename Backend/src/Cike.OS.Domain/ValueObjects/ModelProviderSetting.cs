namespace Cike.OS.Domain.ValueObjects;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(OpenAIModelProviderSetting), (int)ModelProviderType.OpenAI)]
[JsonDerivedType(typeof(AnthropicModelProviderSetting), (int)ModelProviderType.Anthropic)]
public abstract class ModelProviderSetting : ValueObject
{
    public ModelProviderType Type { get; set; }

    public string BaseUrl { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BaseUrl;
        yield return ApiKey;
    }
}

public class OpenAIModelProviderSetting : ModelProviderSetting
{
    public string Organization { get; set; } = null!;
}

public class AnthropicModelProviderSetting : ModelProviderSetting
{
    public string Version { get; set; } = null!;
}
