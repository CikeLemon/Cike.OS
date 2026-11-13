using System;
using System.Collections.Generic;
using System.Text;

namespace Cike.OS.Domain.Entities;

public class Tool : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public ToolType Type { get; set; }

    public long ToolPackageId { get; set; }
}
