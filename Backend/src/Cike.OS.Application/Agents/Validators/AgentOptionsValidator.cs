using System.Text.Json;
using Cike.OS.Domain.Shared.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.AI;

namespace Cike.OS.Application.Agents.Validators;

/// <summary>
/// 模型选项的校验（规则已逐条经用户确认）：全部可空，空 = 不设置；
/// 是否被所选模型支持不在保存时校验，运行时由框架按模型能力做 best-effort 映射。
/// </summary>
public class AgentOptionsValidator : AbstractValidator<AgentOptions>
{
    public AgentOptionsValidator()
    {
        RuleFor(x => x.Temperature)
            .InclusiveBetween(0f, 2f).WithMessage("温度须在 0 到 2 之间。");

        RuleFor(x => x.TopP)
            .InclusiveBetween(0f, 1f).WithMessage("TopP 须在 0 到 1 之间。");

        RuleFor(x => x.TopK)
            .GreaterThanOrEqualTo(1).WithMessage("TopK 须大于等于 1。");

        RuleFor(x => x.MaxOutputTokens)
            .GreaterThanOrEqualTo(1).WithMessage("输出上限须大于等于 1。");

        RuleFor(x => x.FrequencyPenalty)
            .InclusiveBetween(-2f, 2f).WithMessage("频率惩罚须在 -2 到 2 之间。");

        RuleFor(x => x.PresencePenalty)
            .InclusiveBetween(-2f, 2f).WithMessage("存在惩罚须在 -2 到 2 之间。");

        RuleFor(x => x.Seed)
            .GreaterThanOrEqualTo(0L).WithMessage("Seed 须大于等于 0。");

        RuleFor(x => x.StopSequences)
            .Must(x => x == null || (x.Count <= 16 && x.All(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 64)))
            .WithMessage("停止词最多 16 条，每条不能为空且不超过 64 个字符。");

        // 推理力度 / 推理输出各自可空（null = 用提供方默认），只校验设置了的那一个
        RuleFor(x => x.Reasoning!.Effort)
            .Must(x => !x.HasValue || Enum.IsDefined(x.Value))
            .When(x => x.Reasoning != null)
            .WithMessage("推理力度无效。");

        RuleFor(x => x.Reasoning!.Output)
            .Must(x => !x.HasValue || Enum.IsDefined(x.Value))
            .When(x => x.Reasoning != null)
            .WithMessage("推理输出无效。");

        RuleFor(x => x.ResponseFormat!.Type)
            .IsInEnum()
            .When(x => x.ResponseFormat != null)
            .WithMessage("响应格式类型无效。");

        RuleFor(x => x.ResponseFormat!.JsonSchemaDescriptor)
            .NotEmpty().WithMessage("响应格式为 JSON 时必须填写 JSON Schema。")
            .Must((_, value) => IsValidJson(value)).WithMessage("JSON Schema 不是合法的 JSON。")
            .When(x => x.ResponseFormat?.Type == AgentChatResponseFormatType.Json);

        // 工具模式是 M.E.AI 的多态对象（$type 判别），只接受 none / auto / required 三种预定义模式
        RuleFor(x => x.ToolMode)
            .Must(x => x is AutoChatToolMode or NoneChatToolMode or RequiredChatToolMode)
            .When(x => x.ToolMode != null)
            .WithMessage("工具模式只能是自动、不使用或必须使用。");
    }

    private static bool IsValidJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
