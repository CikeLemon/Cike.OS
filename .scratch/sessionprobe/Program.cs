using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;

// ===== 1) 单条 ChatMessage 的 JSON 形状（含工具调用） =====
var userMsg = new ChatMessage(ChatRole.User, "帮我创建一张出差报销审批单")
{
    AuthorName = "xigua",
    MessageId = "m-001",
    CreatedAt = DateTimeOffset.Parse("2026-11-17T10:00:00Z"),
};

var assistantCallMsg = new ChatMessage(ChatRole.Assistant,
[
    new TextContent("好的，我来调用 OA 工具创建审批单"),
    new FunctionCallContent("call-001", "createApproval", new Dictionary<string, object?> { ["title"] = "出差报销", ["amount"] = 3000 }),
])
{
    AuthorName = "报销Agent",
    CreatedAt = DateTimeOffset.Parse("2026-11-17T10:00:01Z"),
};

var toolResultMsg = new ChatMessage(ChatRole.Tool,
[
    new FunctionResultContent("call-001", new { ok = true, approvalId = "AP-2026-001" }),
])
{
    CreatedAt = DateTimeOffset.Parse("2026-11-17T10:00:02Z"),
};

var opts = new JsonSerializerOptions { WriteIndented = true };
Console.WriteLine("===== 用户消息 JSON =====");
Console.WriteLine(JsonSerializer.Serialize(userMsg, opts));
Console.WriteLine();
Console.WriteLine("===== 助手消息（含工具调用）JSON =====");
Console.WriteLine(JsonSerializer.Serialize(assistantCallMsg, opts));
Console.WriteLine();
Console.WriteLine("===== 工具结果消息 JSON =====");
Console.WriteLine(JsonSerializer.Serialize(toolResultMsg, opts));
Console.WriteLine();

// ===== 2) 真实跑一次 Agent，序列化完整 AgentSession blob =====
var agent = new ChatClientAgent(new EchoChatClient());
var session = await agent.CreateSessionAsync();

// 第一轮：用户说话，agent 回文本
await agent.RunAsync("我要申请出差报销", session);
// 第二轮：agent 回复里带工具调用
await agent.RunAsync("直接创建吧", session);

var blob = await agent.SerializeSessionAsync(session);
Console.WriteLine("===== 完整 AgentSession 序列化 blob =====");
Console.WriteLine(blob.GetRawText());

internal sealed class EchoChatClient : IChatClient
{
    private int _turn;

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ChatMessage message;
        if (Interlocked.Increment(ref this._turn) == 1)
        {
            message = new ChatMessage(ChatRole.Assistant, "请提供报销事由与金额。") { AuthorName = "报销Agent" };
        }
        else
        {
            message = new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("正在调用 OA 创建审批单"),
                new FunctionCallContent("call-001", "createApproval", new Dictionary<string, object?> { ["title"] = "出差报销", ["amount"] = 3000 }),
            ])
            { AuthorName = "报销Agent" };
        }

        return Task.FromResult(new ChatResponse([message])
        {
            CreatedAt = DateTimeOffset.Now,
            Usage = new UsageDetails
            {
                InputTokenCount = 120,
                OutputTokenCount = 35,
                TotalTokenCount = 155,
            },
        });
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent("流式占位")],
        };
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
