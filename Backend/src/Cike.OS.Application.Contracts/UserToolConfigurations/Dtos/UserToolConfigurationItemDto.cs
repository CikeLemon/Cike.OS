using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;

/// <summary>工具参数页的一张卡片：当前用户对某个工具的配置，连同工具 / 工具包 / 分类的信息与参数定义（供「缺必填」判定与显示）。</summary>
public class UserToolConfigurationItemDto : AuditedEntityDto<long>
{
    public long ToolId { get; set; }

    public string ToolName { get; set; } = null!;

    public ToolType ToolType { get; set; }

    public string ToolPackageName { get; set; } = null!;

    public string ToolPackageCategoryName { get; set; } = null!;

    /// <summary>工具声明的参数定义；敏感值原样返回，遮罩在前端。</summary>
    public List<ToolParameterDefinition> Parameters { get; set; } = [];

    public Dictionary<string, string> Values { get; set; } = [];
}
