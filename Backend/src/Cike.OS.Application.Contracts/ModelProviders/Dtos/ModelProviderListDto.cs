using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>模型提供方列表的一行：界面要什么给什么，不是详情 DTO 的复用。</summary>
public class ModelProviderListDto
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>名下接入点用到的协议，去重，按协议枚举顺序。</summary>
    public List<ModelProviderProtocol> Protocols { get; set; } = [];

    /// <summary>跨接入点按模型去重的显示名称，分数高的排前面。</summary>
    public List<string> ModelDisplayNames { get; set; } = [];

    public int EndpointCount { get; set; }

    public DateTime UpdatedAt { get; set; }
}
