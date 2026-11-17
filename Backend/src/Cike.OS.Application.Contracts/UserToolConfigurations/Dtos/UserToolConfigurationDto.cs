using Cike.Contracts.EntityDtos;

namespace Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;

public class UserToolConfigurationDto : AuditedEntityDto<long>
{
    public long ToolId { get; set; }

    public Dictionary<string, string> Values { get; set; } = [];
}
