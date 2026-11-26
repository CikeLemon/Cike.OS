namespace Cike.OS.Application.Contracts.Models.Dtos;

public class CreateModelDto
{
    /// <summary>模型名称（如 glm-5.3）：创建后不可变。</summary>
    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    /// <summary>可选，未填写为空字符串。</summary>
    public string? Description { get; set; } = string.Empty;

    public List<long> TagIds { get; set; } = [];
}
