using Cike.OS.Domain.ValueObjects;

namespace Cike.OS.Domain.Entities;

public class UserToolConfiguration : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public long ToolId { get; set; }

    public Dictionary<string, string> Values { get; set; } = [];
}
