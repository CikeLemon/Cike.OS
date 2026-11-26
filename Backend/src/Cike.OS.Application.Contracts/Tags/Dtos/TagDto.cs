using Cike.Contracts.EntityDtos;

namespace Cike.OS.Application.Contracts.Tags.Dtos;

public class TagDto : AuditedEntityDto<long>
{
    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;
}
