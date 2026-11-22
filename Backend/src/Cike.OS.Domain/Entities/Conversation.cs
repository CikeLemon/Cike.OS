using System;
using System.Collections.Generic;
using System.Text;

namespace Cike.OS.Domain.Entities;

public class Conversation : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Title { get; set; } = null!;

    public ConversationType Type { get; set; }

    /// <summary>
    /// 根据type区分
    /// </summary>
    public long AgentId { get; set; }
}
