namespace Cike.OS.Domain.Shared.Enums;

/// <summary>
/// 协议：接入点与模型服务通信所遵循的接口约定（「怎么讲」），不是供应商（「谁」）。
/// 同一供应商可以提供多种协议的接入点。数值同时是协议专属设置的 JSON 判别键取值。
/// </summary>
public enum ModelProviderProtocol
{
    OpenAI = 1,
    Anthropic = 2,
}
