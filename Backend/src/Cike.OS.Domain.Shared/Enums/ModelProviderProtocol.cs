namespace Cike.OS.Domain.Shared.Enums;

/// <summary>
/// 协议：模型提供方与模型服务通信所遵循的接口约定（「怎么讲」），不是供应商（「谁」）。
/// 同一供应商有多种协议时各建一个模型提供方。数值同时是协议专属设置的 JSON 判别键取值，只增不改。
/// </summary>
public enum ModelProviderProtocol
{
    /// <summary>OpenAI Chat Completion 协议（/chat/completions）。</summary>
    OpenAIChatCompletion = 1,
    Anthropic = 2,
    /// <summary>OpenAI Response 协议（/responses）。</summary>
    OpenAIResponse = 3,
}
