namespace Cike.OS.Domain.Entities;

public class ConversationMember : FullAuditedEntity<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public long ConversationId { get; set; }

    public long MemberId { get; set; }

    public ConversationMemberRole Role { get; set; }
}
