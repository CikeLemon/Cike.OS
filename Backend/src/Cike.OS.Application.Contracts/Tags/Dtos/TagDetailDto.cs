using Cike.Contracts.EntityDtos;

namespace Cike.OS.Application.Contracts.Tags.Dtos;

/// <summary>标签详情：编辑表单回填用。</summary>
public class TagDetailDto : AuditedEntityDto<long>
{
    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;
}
