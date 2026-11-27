using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Validators;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.Json;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.Tests.Infrastructure;
using FluentValidation.TestHelper;
using System.Text.Json;
using Xunit;

namespace Cike.OS.Tests;

/// <summary>模型提供方新建输入的校验：连接字段必填、协议专属字段可选、设置的子类必须与协议对应、模型列表可以为空。</summary>
public class CreateModelProviderDtoValidatorTests
{
    private readonly CreateModelProviderDtoValidator _validator = new();

    private static CreateModelProviderDto Valid(Action<CreateModelProviderDto>? mutate = null)
    {
        var dto = TestDataFactory.Provider();
        mutate?.Invoke(dto);
        return dto;
    }

    [Fact]
    public void Validate_ValidProvider_HasNoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Name = "")).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_TooLongName_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Name = new string('x', 129))).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_UnknownProtocol_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Protocol = (ModelProviderProtocol)99)).ShouldHaveValidationErrorFor(x => x.Protocol);
    }

    [Fact]
    public void Validate_MissingBaseUrl_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.BaseUrl = " ")).ShouldHaveValidationErrorFor(x => x.BaseUrl);
    }

    [Fact]
    public void Validate_BaseUrlThatIsNotAUrl_HasNoErrors()
    {
        // 供应商的兼容地址写法不统一，只校验必填、不校验 URL 格式
        _validator.TestValidate(Valid(dto => dto.BaseUrl = "localhost:11434/v1")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MissingApiKey_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.ApiKey = "")).ShouldHaveValidationErrorFor(x => x.ApiKey);
    }

    [Fact]
    public void Validate_LongApiKey_HasNoErrors()
    {
        _validator.TestValidate(Valid(dto => dto.ApiKey = new string('k', 4096))).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NullSetting_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Setting = null!)).ShouldHaveValidationErrorFor("Setting");
    }

    [Fact]
    public void Validate_ProtocolMismatch_HasError()
    {
        // 子类由判别键决定：声明 OpenAI 协议却带着 Anthropic 的设置
        _validator.TestValidate(Valid(dto => dto.Setting = new AnthropicModelProviderSetting { Version = "2023-06-01" }))
            .ShouldHaveValidationErrorFor("Setting");
    }

    [Fact]
    public void Validate_AnthropicWithProtocolSpecificField_HasNoErrors()
    {
        _validator.TestValidate(Valid(dto =>
        {
            dto.Protocol = ModelProviderProtocol.Anthropic;
            dto.Setting = new AnthropicModelProviderSetting { Version = "2023-06-01" };
        })).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_OpenAIResponseWithItsSetting_HasNoErrors()
    {
        _validator.TestValidate(Valid(dto =>
        {
            dto.Protocol = ModelProviderProtocol.OpenAIResponse;
            dto.Setting = new OpenAIResponseModelProviderSetting { Organization = "org-1" };
        })).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_OpenAIProtocolsHaveDistinctSettings_MismatchHasError()
    {
        // 两种 OpenAI 协议各自对应一个设置子类，互换即不匹配
        _validator.TestValidate(Valid(dto => dto.Protocol = ModelProviderProtocol.OpenAIResponse))
            .ShouldHaveValidationErrorFor("Setting");

        _validator.TestValidate(Valid(dto => dto.Setting = new OpenAIResponseModelProviderSetting()))
            .ShouldHaveValidationErrorFor("Setting");
    }

    [Fact]
    public void Deserialize_UnknownDiscriminator_Throws()
    {
        // 判别键未知：STJ 反序列化直接失败（HTTP 层 400），根本到不了 Validator
        Assert.ThrowsAny<JsonException>(() =>
            JsonHelper.Deserialize<ModelProviderSetting>("""{"type":99,"organization":"o"}"""));
    }

    [Fact]
    public void Validate_NullModels_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Models = null!)).ShouldHaveValidationErrorFor(x => x.Models);
    }

    [Fact]
    public void Validate_EmptyModels_HasNoErrors()
    {
        _validator.TestValidate(Valid(dto => dto.Models = [])).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ModelWithoutName_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Models = [TestDataFactory.Model("")]))
            .ShouldHaveValidationErrorFor("Models[0].Name");
    }

    [Fact]
    public void Validate_TooLongModelName_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Models = [TestDataFactory.Model(new string('x', 129))]))
            .ShouldHaveValidationErrorFor("Models[0].Name");
    }

    [Fact]
    public void Validate_TooLongDescription_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Models = [TestDataFactory.Model("glm-5.3", new string('d', 2049))]))
            .ShouldHaveValidationErrorFor("Models[0].Description");
    }

    [Fact]
    public void Validate_NullTagIds_HasError()
    {
        var model = TestDataFactory.Model("glm-5.3");
        model.TagIds = null!;

        _validator.TestValidate(Valid(dto => dto.Models = [model])).ShouldHaveValidationErrorFor("Models[0].TagIds");
    }

    [Fact]
    public void Validate_TagIdZero_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Models = [TestDataFactory.Model("glm-5.3", tagIds: [0])]))
            .ShouldHaveValidationErrorFor("Models[0].TagIds[0]");
    }
}

/// <summary>模型提供方更新输入的校验：与新建同形（协议可改，所以也在请求体里）。</summary>
public class UpdateModelProviderDtoValidatorTests
{
    private readonly UpdateModelProviderDtoValidator _validator = new();

    private static UpdateModelProviderDto Valid(Action<UpdateModelProviderDto>? mutate = null)
    {
        var dto = TestDataFactory.ProviderUpdate();
        mutate?.Invoke(dto);
        return dto;
    }

    [Fact]
    public void Validate_ValidProvider_HasNoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Name = "")).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_MissingBaseUrlAndApiKey_HasErrors()
    {
        var result = _validator.TestValidate(Valid(dto =>
        {
            dto.BaseUrl = " ";
            dto.ApiKey = "";
        }));

        result.ShouldHaveValidationErrorFor(x => x.BaseUrl);
        result.ShouldHaveValidationErrorFor(x => x.ApiKey);
    }

    [Fact]
    public void Validate_UnknownProtocol_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Protocol = (ModelProviderProtocol)99)).ShouldHaveValidationErrorFor(x => x.Protocol);
    }

    [Fact]
    public void Validate_NullSetting_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Setting = null!)).ShouldHaveValidationErrorFor("Setting");
    }

    [Fact]
    public void Validate_ProtocolMismatch_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Setting = new AnthropicModelProviderSetting()))
            .ShouldHaveValidationErrorFor("Setting");
    }

    [Fact]
    public void Validate_NullModels_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Models = null!)).ShouldHaveValidationErrorFor(x => x.Models);
    }

    [Fact]
    public void Validate_ModelWithoutName_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Models = [TestDataFactory.Model("")]))
            .ShouldHaveValidationErrorFor("Models[0].Name");
    }
}
