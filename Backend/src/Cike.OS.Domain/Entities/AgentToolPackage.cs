namespace Cike.OS.Domain.Entities;

public class AgentToolPackage : AuditedEntity<long>
{
    public long AgentId { get; set; }

    public long ToolPackageId { get; set; }

    public bool IsFull { get; set; }

    public List<AgentTool> AgentTools { get; set; } = [];
}
