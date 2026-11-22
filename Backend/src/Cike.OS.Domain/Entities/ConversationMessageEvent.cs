using System;
using System.Collections.Generic;
using System.Text;

namespace Cike.OS.Domain.Entities;

public class ConversationMessageEvent : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public long ConversationId { get; set; }

    public long ConversationMessageId { get; set; }

    public string ActionId { get; set; } = string.Empty;

    public string ActionName { get; set; } = string.Empty;

    public string Type { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int Duration { get; set; }
}
