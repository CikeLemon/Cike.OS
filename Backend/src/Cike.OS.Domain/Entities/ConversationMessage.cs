using Microsoft.Extensions.AI;

namespace Cike.OS.Domain.Entities;

public class ConversationMessage : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public long ConversationId { get; set; }

    public ChatRole SenderRole { get; set; }

    public long SenderId { get; set; }
}
