using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cike.OS.Domain.ValueObjects;

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

public class AgentChatResponseFormat
{
    public AgentChatResponseFormatType Type { get; set; }

    public string? JsonSchemaName { get; set; }

    public string? JsonSchemaDescriptor { get; set; }
}
