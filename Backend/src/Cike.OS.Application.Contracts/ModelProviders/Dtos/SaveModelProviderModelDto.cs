namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>模型的一条输入（随模型提供方提交，以名称为键，不带 Id）。</summary>
public class SaveModelProviderModelDto
{
    public string Name { get; set; } = null!;

    /// <summary>可选，未填写为空字符串。</summary>
    public string? Description { get; set; }

    public List<long> TagIds { get; set; } = [];
}
