namespace Cike.OS.Domain.Shared.ValueObjects;

public class ToolParameterDefinition
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool Required { get; set; }

    public bool Sensitive { get; set; }
}
