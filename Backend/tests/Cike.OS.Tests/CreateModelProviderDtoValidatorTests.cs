using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Validators;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.Json;
using Cike.OS.Domain.Shared.ValueObjects;
using FluentValidation.TestHelper;
using System.Text.Json;
using Xunit;

namespace Cike.OS.Tests;

public class CreateModelProviderDtoValidatorTests
{
    private readonly CreateModelProviderDtoValidator _validator = new();

    private static CreateModelProviderDto Valid() => new()
    {
        Name = "智谱官方",
        Setting = new OpenAIModelProviderSetting
        {
            Type = ModelProviderType.OpenAI,
            BaseUrl = "https://api.openai.com/v1",
            ApiKey = "sk-test",
            Organization = "org-1",
        },
    };

    [Fact]
    public void Validate_ValidProvider_HasNoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NullSetting_HasError()
    {
        var dto = Valid();
        dto.Setting = null!;

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting");
    }

    [Fact]
    public void Validate_MissingBaseUrl_HasError()
    {
        var dto = Valid();
        ((OpenAIModelProviderSetting)dto.Setting).BaseUrl = " ";

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting.BaseUrl");
    }

    [Fact]
    public void Validate_MissingApiKey_HasError()
    {
        var dto = Valid();
        ((OpenAIModelProviderSetting)dto.Setting).ApiKey = "";

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting.ApiKey");
    }

    [Fact]
    public void Validate_OpenAIWithoutOrganization_HasError()
    {
        var dto = Valid();
        ((OpenAIModelProviderSetting)dto.Setting).Organization = "";

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting.Organization");
    }

    [Fact]
    public void Validate_AnthropicWithoutVersion_HasError()
    {
        var dto = Valid();
        dto.Setting = new AnthropicModelProviderSetting
        {
            Type = ModelProviderType.Anthropic,
            BaseUrl = "https://api.anthropic.com",
            ApiKey = "ak",
            Version = " ",
        };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting.Version");
    }

    [Fact]
    public void Validate_TypeMismatch_HasError()
    {
        // 防御性分支：Type 由子类构造函数赋值且不进 JSON，wire 上不可能出现「子类与 Type 不一致」；
        // 真正可达的「类型不合法」是判别键未知 → 反序列化失败（见下一用例）
        var dto = Valid();
        // Anthropic 设置但 Type 标成 OpenAI：类型与子类型不匹配
        dto.Setting = new AnthropicModelProviderSetting
        {
            Type = ModelProviderType.OpenAI,
            BaseUrl = "https://api.anthropic.com",
            ApiKey = "ak",
            Version = "2023-06-01",
        };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting");
    }

    [Fact]
    public void Deserialize_UnknownDiscriminator_Throws()
    {
        // 判别键未知：STJ 反序列化直接失败（HTTP 层 400），根本到不了 Validator
        Assert.ThrowsAny<JsonException>(() =>
            JsonHelper.Deserialize<ModelProviderSetting>("""{"type":99,"baseUrl":"u","apiKey":"k"}"""));
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        var dto = Valid();
        dto.Name = "";

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NullMappings_HasError()
    {
        var dto = Valid();
        dto.Mappings = null!;

        // RuleForEach 对 null 集合不报错，必须靠 NotNull 拦下，否则 Handler 会 NRE
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Mappings);
    }

    [Fact]
    public void Validate_MappingWithoutModel_HasError()
    {
        var dto = Valid();
        dto.Mappings = [new SaveModelProviderMappingDto { ModelId = 0, Priority = 10 }];

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Mappings[0].ModelId");
    }

    [Fact]
    public void Validate_TooLongProviderModelName_HasError()
    {
        var dto = Valid();
        dto.Mappings = [new SaveModelProviderMappingDto { ModelId = 1, ProviderModelName = new string('x', 129) }];

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Mappings[0].ProviderModelName");
    }
}
