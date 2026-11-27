namespace Cike.OS.Application.Contracts.Tags.Dtos;

public class CreateTagDto
{
    /// <summary>机器标识（如 vision）：创建后不可变。</summary>
    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;
}
