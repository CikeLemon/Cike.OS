namespace Cike.OS.Application.Contracts.Models.Dtos;

public class UpdateModelDto
{
    public string DisplayName { get; set; } = null!;

    /// <summary>可选，未填写为空字符串。</summary>
    public string? Description { get; set; } = string.Empty;

    public List<long> TagIds { get; set; } = [];
}
