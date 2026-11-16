namespace Cike.OS.Domain.Entities;

public class AgentTool : AuditedEntity<long>
{
    public long AgentToolPackageId { get; set; }

    public long ToolId { get; set; }
}
