using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Application.Contracts.Agents.Dtos;

/// <summary>提交的一条工具绑定：整包（指向工具包）或单个工具。</summary>
public class SaveAgentToolDto
{
    public AgentToolType Type { get; set; }

    public long TargetId { get; set; }
}
