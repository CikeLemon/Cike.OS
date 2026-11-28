using Microsoft.Extensions.AI;

namespace Cike.OS.Domain.Shared.ValueObjects;

/// <summary>
/// Agent 的模型选项（默认值，运行时可调整）。位于 Domain.Shared，直接作为 API 契约与持久化形状：
/// 推理选项与工具模式直接复用 Microsoft.Extensions.AI 的类型，不另建镜像。
/// 全部字段可空，空 = 不设置、由模型自行决定；非空 / 范围等规则一律在 FluentValidation。
/// </summary>
public class AgentOptions
{
    public float? Temperature { get; set; }

    public int? MaxOutputTokens { get; set; }

    public float? TopP { get; set; }

    public int? TopK { get; set; }

    public float? FrequencyPenalty { get; set; }

    public float? PresencePenalty { get; set; }

    public long? Seed { get; set; }

    public ReasoningOptions? Reasoning { get; set; }

    public AgentChatResponseFormat? ResponseFormat { get; set; }

    public List<string>? StopSequences { get; set; }

    public bool? AllowMultipleToolCalls { get; set; }

    public ChatToolMode? ToolMode { get; set; }

    public bool? AllowBackgroundResponses { get; set; }
}

public enum AgentChatResponseFormatType
{
    Text = 1,
    Json = 2,
}

/// <summary>响应格式：文本，或按 JSON Schema 输出（<see cref="JsonSchemaDescriptor"/> 为 Schema 原文）。</summary>
public class AgentChatResponseFormat
{
    public AgentChatResponseFormatType Type { get; set; }

    public string? JsonSchemaName { get; set; }

    public string? JsonSchemaDescriptor { get; set; }
}
