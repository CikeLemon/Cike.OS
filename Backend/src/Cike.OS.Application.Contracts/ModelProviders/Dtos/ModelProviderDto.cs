using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>模型提供方：一条连接，连同它提供的全部模型（按名称排序）。</summary>
public class ModelProviderDto : AuditedEntityDto<long>
{
    public string Name { get; set; } = null!;

    public ModelProviderProtocol Protocol { get; set; }

    public string BaseUrl { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    public ModelProviderSetting Setting { get; set; } = null!;

    public List<ModelDto> Models { get; set; } = [];
}
