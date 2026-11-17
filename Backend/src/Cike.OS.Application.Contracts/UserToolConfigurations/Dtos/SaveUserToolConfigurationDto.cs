namespace Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;

public class SaveUserToolConfigurationDto
{
    public long ToolId { get; set; }

    public Dictionary<string, string> Values { get; set; } = [];
}
