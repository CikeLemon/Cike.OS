namespace Cike.OS.Domain.Entities
{
    public class ToolPackage : FullAuditedAggregateRoot<long>, IMultiTenant
    {
        public long TenantId { get; set; }

        public string Code { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Description { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public string Readme { get; set; } = string.Empty;

        public long ToolPackageCategoryId { get; set; }

        public ToolPackageStatus Status { get; set; }
    }
}
