using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>接入点的输出：随 <see cref="ModelProviderDto"/> 一起带出，没有独立的查询接口。</summary>
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
