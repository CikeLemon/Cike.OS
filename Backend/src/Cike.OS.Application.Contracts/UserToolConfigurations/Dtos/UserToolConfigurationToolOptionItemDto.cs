using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;

/// <summary>批量新增弹窗的候选：每个声明了参数的工具一条，带当前用户是否已配置。</summary>
public class UserToolConfigurationToolOptionItemDto
{
    public long ToolId { get; set; }

    public string ToolName { get; set; } = null!;

    public ToolType ToolType { get; set; }

    public string ToolPackageName { get; set; } = null!;

    public List<ToolParameterDefinition> Parameters { get; set; } = [];

    public bool Configured { get; set; }
}
