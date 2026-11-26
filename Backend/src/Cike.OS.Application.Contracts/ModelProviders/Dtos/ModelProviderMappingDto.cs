namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>提供商映射的输出：随 <see cref="ModelProviderEndpointDto"/> 一起带出，没有独立接口。</summary>
public class ModelProviderMappingDto
{
    public long ModelId { get; set; }

    /// <summary>模型的显示名称（如「GLM 5.3」），由查询侧批量回填，供界面直接展示。</summary>
    public string ModelDisplayName { get; set; } = string.Empty;

    /// <summary>路由分数：分数高的先用，接入点之间平等，不要求唯一。</summary>
    public int Priority { get; set; }

    /// <summary>提供商侧模型名；空字符串表示回落用模型名称。</summary>
    public string ProviderModelName { get; set; } = string.Empty;
}
