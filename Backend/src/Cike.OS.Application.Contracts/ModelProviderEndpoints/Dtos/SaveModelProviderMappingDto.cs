namespace Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;

/// <summary>提供商映射的一条输入：随接入点提交，以 ModelId 为键（已映射的更新、新的新增、未提交的移除）。没有独立接口。</summary>
public class SaveModelProviderMappingDto
{
    public long ModelId { get; set; }

    /// <summary>路由分数：只是排序依据，不要求唯一，分数高的先用。</summary>
    public int Priority { get; set; }

    /// <summary>提供商侧模型名；省略或空白表示回落用模型名称。</summary>
    public string? ProviderModelName { get; set; }
}
