using Cike.Cqrs.Commands;

namespace Cike.OS.Application.Agents.Commands;

/// <summary>删除不做守护：会话仍可查看，只是不能再对话；名下的工具绑定随之软删。</summary>
public record DeleteAgentCommand(long Id) : Command;
