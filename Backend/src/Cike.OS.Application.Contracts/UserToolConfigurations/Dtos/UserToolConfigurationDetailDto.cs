using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;

/// <summary>当前用户对一个工具的参数配置详情：编辑弹窗按工具此刻的参数定义生成字段、用已存的值回填。</summary>
public class UserToolConfigurationDetailDto : AuditedEntityDto<long>
{
    public long ToolId { get; set; }

    public string ToolName { get; set; } = null!;

    /// <summary>工具此刻声明的参数定义。</summary>
    public List<ToolParameterDefinition> Parameters { get; set; } = [];

    public Dictionary<string, string> Values { get; set; } = [];
}
