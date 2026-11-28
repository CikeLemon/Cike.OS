using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Application.Contracts.Tools.Dtos;

/// <summary>工具详情：编辑面板回填（参数定义与形态设置都在这里）。</summary>
public class ToolDetailDto : AuditedEntityDto<long>
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public ToolType Type { get; set; }

    public long ToolPackageId { get; set; }

    public List<ToolParameterDefinition> Parameters { get; set; } = [];

    public ToolSetting Setting { get; set; } = null!;
}
