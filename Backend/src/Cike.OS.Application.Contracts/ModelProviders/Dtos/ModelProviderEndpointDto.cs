using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>接入点的输出：由接入点自己的列表接口带出（<c>GET /ModelProviders/{id}/Endpoints</c>），每个接入点内嵌它的全部映射。</summary>
public class ModelProviderEndpointDto : AuditedEntityDto<long>
{
    public string Name { get; set; } = null!;

    public ModelProviderProtocol Protocol { get; set; }

    public string BaseUrl { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    /// <summary>协议专属设置（多态值对象直接作契约，沿 ADR 0003）。</summary>
    public ModelProviderEndpointSetting Setting { get; set; } = null!;

    public List<ModelProviderMappingDto> Mappings { get; set; } = [];
}
