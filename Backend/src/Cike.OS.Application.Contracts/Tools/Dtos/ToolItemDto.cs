using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Application.Contracts.Tools.Dtos;

/// <summary>工具包详情页的工具表的一行：参数定义只给个数，设置不带。</summary>
public class ToolItemDto : AuditedEntityDto<long>
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public ToolType Type { get; set; }

    /// <summary>声明的参数个数（参数定义是 json 列，取回后在内存里数）。</summary>
    public int ParameterCount { get; set; }
}
