using Cike.Contracts.EntityDtos;

namespace Cike.OS.Application.Contracts.Tags.Dtos;

/// <summary>标签列表的一行（标签管理页的分页列表；模型表单的标签选择器也用它）。</summary>
public class TagItemDto : AuditedEntityDto<long>
{
    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;
}
