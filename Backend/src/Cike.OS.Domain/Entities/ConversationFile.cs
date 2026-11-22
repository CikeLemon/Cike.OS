using System;
using System.Collections.Generic;
using System.Text;

namespace Cike.OS.Domain.Entities;

public class ConversationFile : FullAuditedEntity<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public long ConversationId { get; set; }

    public long Sender { get; set; }

    public string Name { get; set; } = null!;

    public long Size { get; set; }

    public string MimeType { get; set; } = null!;

    public string StoragePath { get; set; } = null!;
}
