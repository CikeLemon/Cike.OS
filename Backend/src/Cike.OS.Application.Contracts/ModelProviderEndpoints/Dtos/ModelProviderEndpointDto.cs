using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;

/// <summary>接入点的输出：列表与详情接口都返回它，每个接入点内嵌它的全部映射。</summary>
public class ModelProviderEndpointDto : AuditedEntityDto<long>
{
    /// <summary>所属的模型提供方：两个聚合按 Id 引用。</summary>
    public long ModelProviderId { get; set; }

    public string Name { get; set; } = null!;

    public ModelProviderProtocol Protocol { get; set; }

    public string BaseUrl { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    /// <summary>协议专属设置（多态值对象直接作契约，沿 ADR 0003）。</summary>
    public ModelProviderEndpointSetting Setting { get; set; } = null!;

    public List<ModelProviderMappingDto> Mappings { get; set; } = [];
}
