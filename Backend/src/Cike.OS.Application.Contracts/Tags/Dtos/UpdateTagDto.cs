namespace Cike.OS.Application.Contracts.Tags.Dtos;

/// <summary>键创建后不可变，不在 UpdateDto 中。</summary>
public class UpdateTagDto
{
    public string Name { get; set; } = null!;
}
