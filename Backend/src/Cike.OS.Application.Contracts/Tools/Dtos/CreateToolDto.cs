using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.Tools.Dtos;

public class CreateToolDto
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public ToolType Type { get; set; }

    public long ToolPackageId { get; set; }

    public List<ToolParameterDefinition> Parameters { get; set; } = [];

    public ToolSetting Setting { get; set; } = null!;
}
