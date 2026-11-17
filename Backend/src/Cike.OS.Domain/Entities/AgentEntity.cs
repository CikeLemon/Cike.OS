namespace Cike.OS.Domain.Entities;

public class AgentEntity : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Instructions { get; set; } = null!;

    public AgentOptions Options { get; set; } = new();
}
