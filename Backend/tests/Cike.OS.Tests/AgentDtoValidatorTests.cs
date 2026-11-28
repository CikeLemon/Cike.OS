using Cike.OS.Application.Agents.Validators;
using Cike.OS.Application.Contracts.Agents.Dtos;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using FluentValidation.TestHelper;
using Microsoft.Extensions.AI;
using Xunit;

namespace Cike.OS.Tests;

/// <summary>Agent 新建输入的校验规则（与更新同形，更新只多验一条「规则确实相同」）。</summary>
public class CreateAgentDtoValidatorTests
{
    private readonly CreateAgentDtoValidator _validator = new();

    private static CreateAgentDto Valid(Action<CreateAgentDto>? mutate = null)
    {
        var dto = new CreateAgentDto
        {
            Name = "客服助手",
            Instructions = "你是一名客服。",
            Options = new AgentOptions(),
            Tools = [],
        };
        mutate?.Invoke(dto);
        return dto;
    }

    private static CreateAgentDto ValidOptions(Action<AgentOptions> mutate) => Valid(dto =>
    {
        dto.Options = new AgentOptions();
        mutate(dto.Options);
    });

    [Fact]
    public void Validate_Valid_HasNoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValidOptionsWithToolMode_HasNoErrors()
    {
        _validator.TestValidate(ValidOptions(options =>
        {
            options.Temperature = 2;
            options.TopP = 1;
            options.TopK = 1;
            options.MaxOutputTokens = 1;
            options.FrequencyPenalty = -2;
            options.PresencePenalty = 2;
            options.Seed = 0;
            options.StopSequences = ["END", "STOP"];
            options.Reasoning = new ReasoningOptions { Effort = ReasoningEffort.High, Output = ReasoningOutput.Summary };
            options.ToolMode = ChatToolMode.RequireAny;
            options.ResponseFormat = new AgentChatResponseFormat { Type = AgentChatResponseFormatType.Json, JsonSchemaDescriptor = "{\"type\":\"object\"}" };
        })).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyName_HasError(string name)
    {
        _validator.TestValidate(Valid(dto => dto.Name = name)).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_TooLongName_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Name = new string('长', 129))).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_TooLongDescription_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Description = new string('描', 513))).ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyInstructions_HasError(string instructions)
    {
        _validator.TestValidate(Valid(dto => dto.Instructions = instructions)).ShouldHaveValidationErrorFor(x => x.Instructions);
    }

    [Fact]
    public void Validate_LongInstructions_HasNoError()
    {
        // 指令不限长度
        _validator.TestValidate(Valid(dto => dto.Instructions = new string('指', 100_000))).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NullOptions_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Options = null!)).ShouldHaveValidationErrorFor(x => x.Options);
    }

    [Fact]
    public void Validate_NullTools_HasError_EmptyToolsOk()
    {
        _validator.TestValidate(Valid(dto => dto.Tools = null!)).ShouldHaveValidationErrorFor(x => x.Tools);
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ToolItem_InvalidTypeOrTarget_HasError()
    {
        _validator.TestValidate(Valid(dto => dto.Tools = [new SaveAgentToolDto { Type = (AgentToolType)9, TargetId = 1 }]))
            .ShouldHaveValidationErrorFor("Tools[0].Type");
        _validator.TestValidate(Valid(dto => dto.Tools = [new SaveAgentToolDto { Type = AgentToolType.Tool, TargetId = 0 }]))
            .ShouldHaveValidationErrorFor("Tools[0].TargetId");
    }

    [Theory]
    [InlineData(2.01f)]
    [InlineData(-0.01f)]
    public void Validate_TemperatureOutOfRange_HasError(float temperature)
    {
        _validator.TestValidate(ValidOptions(options => options.Temperature = temperature)).ShouldHaveValidationErrorFor(x => x.Options.Temperature);
    }

    [Theory]
    [InlineData(1.01f)]
    [InlineData(-0.01f)]
    public void Validate_TopPOutOfRange_HasError(float topP)
    {
        _validator.TestValidate(ValidOptions(options => options.TopP = topP)).ShouldHaveValidationErrorFor(x => x.Options.TopP);
    }

    [Fact]
    public void Validate_TopKOrMaxOutputTokensBelowOne_HasError()
    {
        _validator.TestValidate(ValidOptions(options => options.TopK = 0)).ShouldHaveValidationErrorFor(x => x.Options.TopK);
        _validator.TestValidate(ValidOptions(options => options.MaxOutputTokens = 0)).ShouldHaveValidationErrorFor(x => x.Options.MaxOutputTokens);
    }

    [Theory]
    [InlineData(-2.01f)]
    [InlineData(2.01f)]
    public void Validate_PenaltiesOutOfRange_HasError(float penalty)
    {
        _validator.TestValidate(ValidOptions(options => options.FrequencyPenalty = penalty)).ShouldHaveValidationErrorFor(x => x.Options.FrequencyPenalty);
        _validator.TestValidate(ValidOptions(options => options.PresencePenalty = penalty)).ShouldHaveValidationErrorFor(x => x.Options.PresencePenalty);
    }

    [Fact]
    public void Validate_NegativeSeed_HasError()
    {
        _validator.TestValidate(ValidOptions(options => options.Seed = -1)).ShouldHaveValidationErrorFor(x => x.Options.Seed);
    }

    [Fact]
    public void Validate_StopSequences_TooMany_OrBlank_OrTooLong_HasError()
    {
        _validator.TestValidate(ValidOptions(options => options.StopSequences = Enumerable.Repeat("a", 17).ToList()))
            .ShouldHaveValidationErrorFor(x => x.Options.StopSequences);
        _validator.TestValidate(ValidOptions(options => options.StopSequences = ["ok", " "]))
            .ShouldHaveValidationErrorFor(x => x.Options.StopSequences);
        _validator.TestValidate(ValidOptions(options => options.StopSequences = [new string('a', 65)]))
            .ShouldHaveValidationErrorFor(x => x.Options.StopSequences);
        _validator.TestValidate(ValidOptions(options => options.StopSequences = Enumerable.Repeat("a", 16).ToList()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Reasoning_EffortOrOutputAloneIsValid_InvalidEnum_HasError()
    {
        // 两个都可空：只设力度、只设输出都合法
        _validator.TestValidate(ValidOptions(options => options.Reasoning = new ReasoningOptions { Effort = ReasoningEffort.High })).ShouldNotHaveAnyValidationErrors();
        _validator.TestValidate(ValidOptions(options => options.Reasoning = new ReasoningOptions { Output = ReasoningOutput.Full })).ShouldNotHaveAnyValidationErrors();

        _validator.TestValidate(ValidOptions(options => options.Reasoning = new ReasoningOptions { Effort = (ReasoningEffort)42 }))
            .ShouldHaveValidationErrorFor(x => x.Options.Reasoning!.Effort);
        _validator.TestValidate(ValidOptions(options => options.Reasoning = new ReasoningOptions { Output = (ReasoningOutput)42 }))
            .ShouldHaveValidationErrorFor(x => x.Options.Reasoning!.Output);
    }

    [Fact]
    public void Validate_ResponseFormat_InvalidType_HasError()
    {
        _validator.TestValidate(ValidOptions(options => options.ResponseFormat = new AgentChatResponseFormat { Type = (AgentChatResponseFormatType)0 }))
            .ShouldHaveValidationErrorFor(x => x.Options.ResponseFormat!.Type);
    }

    [Fact]
    public void Validate_ResponseFormatJson_WithoutSchema_OrInvalidJson_HasError()
    {
        _validator.TestValidate(ValidOptions(options => options.ResponseFormat = new AgentChatResponseFormat { Type = AgentChatResponseFormatType.Json }))
            .ShouldHaveValidationErrorFor(x => x.Options.ResponseFormat!.JsonSchemaDescriptor);
        _validator.TestValidate(ValidOptions(options => options.ResponseFormat = new AgentChatResponseFormat { Type = AgentChatResponseFormatType.Json, JsonSchemaDescriptor = "{not json" }))
            .ShouldHaveValidationErrorFor(x => x.Options.ResponseFormat!.JsonSchemaDescriptor);
        // 文本格式不要求 Schema
        _validator.TestValidate(ValidOptions(options => options.ResponseFormat = new AgentChatResponseFormat { Type = AgentChatResponseFormatType.Text }))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ToolMode_OnlyPredefinedModes_HasNoError_OthersRejected()
    {
        // 这里只能构造出三种预定义模式与 RequireSpecific（同为 required 判别，放行）；
        // 非法的 $type 在反序列化阶段就被拒绝，由 HTTP 测试覆盖
        _validator.TestValidate(ValidOptions(options => options.ToolMode = ChatToolMode.None)).ShouldNotHaveAnyValidationErrors();
        _validator.TestValidate(ValidOptions(options => options.ToolMode = ChatToolMode.RequireSpecific("search"))).ShouldNotHaveAnyValidationErrors();
    }
}

public class UpdateAgentDtoValidatorTests
{
    private readonly UpdateAgentDtoValidator _validator = new();

    [Fact]
    public void Validate_SameRulesAsCreate()
    {
        var dto = new UpdateAgentDto { Name = "", Instructions = "", Options = new AgentOptions { Temperature = 9 }, Tools = null! };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Instructions);
        result.ShouldHaveValidationErrorFor(x => x.Options.Temperature);
        result.ShouldHaveValidationErrorFor(x => x.Tools);

        _validator.TestValidate(new UpdateAgentDto { Name = "ok", Instructions = "ok", Options = new AgentOptions(), Tools = [] })
            .ShouldNotHaveAnyValidationErrors();
    }
}
