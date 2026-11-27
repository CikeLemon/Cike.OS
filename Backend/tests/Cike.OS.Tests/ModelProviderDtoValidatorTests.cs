using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Application.ModelProviders.Validators;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.Json;
using Cike.OS.Domain.Shared.ValueObjects;
using FluentValidation.TestHelper;
using System.Text.Json;
using Xunit;

namespace Cike.OS.Tests;

/// <summary>模型提供方的输入校验：名称；更新时还带全部接入点。</summary>
public class ModelProviderDtoValidatorTests
{
    [Fact]
    public void Validate_ValidName_HasNoErrors()
    {
        new CreateModelProviderDtoValidator().TestValidate(new CreateModelProviderDto { Name = "智谱" })
            .ShouldNotHaveAnyValidationErrors();
        new UpdateModelProviderDtoValidator().TestValidate(new UpdateModelProviderDto { Name = "智谱" })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        new CreateModelProviderDtoValidator().TestValidate(new CreateModelProviderDto { Name = "" })
            .ShouldHaveValidationErrorFor(x => x.Name);
        new UpdateModelProviderDtoValidator().TestValidate(new UpdateModelProviderDto { Name = " " })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_TooLongName_HasError()
    {
        var name = new string('x', 129);

        new CreateModelProviderDtoValidator().TestValidate(new CreateModelProviderDto { Name = name })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NullEndpoints_HasError()
    {
        var dto = new UpdateModelProviderDto { Name = "智谱", Endpoints = null! };

        // RuleForEach 对 null 集合不报错，必须靠 NotNull 拦下，否则 Handler 会 NRE
        new UpdateModelProviderDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Endpoints);
    }

    [Fact]
    public void Validate_EndpointErrors_AreReportedUnderTheirIndex()
    {
        var dto = new UpdateModelProviderDto
        {
            Name = "智谱",
            Endpoints = [new SaveModelProviderEndpointDto { Name = "", Protocol = ModelProviderProtocol.OpenAIChatCompletion, BaseUrl = "https://x", ApiKey = "k", Setting = new OpenAIChatCompletionModelProviderEndpointSetting() }],
        };

        new UpdateModelProviderDtoValidator().TestValidate(dto).ShouldHaveValidationErrorFor("Endpoints[0].Name");
    }
}

/// <summary>接入点输入的校验：连接字段必填，协议专属字段可选，设置的子类必须与协议对应。</summary>
public class SaveModelProviderEndpointDtoValidatorTests
{
    private readonly SaveModelProviderEndpointDtoValidator _validator = new();

    private static SaveModelProviderEndpointDto Valid() => new()
    {
        Name = "官方",
        Protocol = ModelProviderProtocol.OpenAIChatCompletion,
        BaseUrl = "https://api.openai.com/v1",
        ApiKey = "sk-test",
        // 协议专属字段不填也合法
        Setting = new OpenAIChatCompletionModelProviderEndpointSetting(),
    };

    [Fact]
    public void Validate_ValidEndpoint_HasNoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        var dto = Valid();
        dto.Name = "";

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_UnknownProtocol_HasError()
    {
        var dto = Valid();
        dto.Protocol = (ModelProviderProtocol)99;

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Protocol);
    }

    [Fact]
    public void Validate_MissingBaseUrl_HasError()
    {
        var dto = Valid();
        dto.BaseUrl = " ";

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.BaseUrl);
    }

    [Fact]
    public void Validate_MissingApiKey_HasError()
    {
        var dto = Valid();
        dto.ApiKey = "";

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.ApiKey);
    }

    [Fact]
    public void Validate_NullSetting_HasError()
    {
        var dto = Valid();
        dto.Setting = null!;

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting");
    }

    [Fact]
    public void Validate_ProtocolMismatch_HasError()
    {
        // 子类由判别键决定：声明 OpenAI 协议却带着 Anthropic 的设置
        var dto = Valid();
        dto.Setting = new AnthropicModelProviderEndpointSetting { Version = "2023-06-01" };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting");
    }

    [Fact]
    public void Validate_AnthropicWithProtocolSpecificField_HasNoErrors()
    {
        var dto = Valid();
        dto.Protocol = ModelProviderProtocol.Anthropic;
        dto.Setting = new AnthropicModelProviderEndpointSetting { Version = "2023-06-01" };

        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_OpenAIResponseWithItsSetting_HasNoErrors()
    {
        var dto = Valid();
        dto.Protocol = ModelProviderProtocol.OpenAIResponse;
        dto.Setting = new OpenAIResponseModelProviderEndpointSetting { Organization = "org-1" };

        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_OpenAIProtocolsHaveDistinctSettings_MismatchHasError()
    {
        // 两种 OpenAI 协议各自对应一个设置子类，互换即不匹配
        var responseWithChatSetting = Valid();
        responseWithChatSetting.Protocol = ModelProviderProtocol.OpenAIResponse;
        _validator.TestValidate(responseWithChatSetting).ShouldHaveValidationErrorFor("Setting");

        var chatWithResponseSetting = Valid();
        chatWithResponseSetting.Setting = new OpenAIResponseModelProviderEndpointSetting();
        _validator.TestValidate(chatWithResponseSetting).ShouldHaveValidationErrorFor("Setting");
    }

    [Fact]
    public void Deserialize_UnknownDiscriminator_Throws()
    {
        // 判别键未知：STJ 反序列化直接失败（HTTP 层 400），根本到不了 Validator
        Assert.ThrowsAny<JsonException>(() =>
            JsonHelper.Deserialize<ModelProviderEndpointSetting>("""{"type":99,"organization":"o"}"""));
    }

    [Fact]
    public void Validate_NullMappings_HasError()
    {
        var dto = Valid();
        dto.Mappings = null!;

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
