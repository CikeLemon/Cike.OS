using Cike.Contracts.EntityDtos;

namespace Cike.OS.Application.Contracts.Models.Dtos;

public class ModelDto : AuditedEntityDto<long>
{
    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public List<long> TagIds { get; set; } = [];
}
