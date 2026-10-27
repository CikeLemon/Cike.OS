using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Domain.Entities;

public class ModelProvider : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Name { get; set; } = null!;

    public ModelProviderType Type { get; set; }
}
