using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>模型提供方列表的一行：界面要什么给什么，不是详情 DTO 的复用。</summary>
public class ModelProviderListDto : AuditedEntityDto<long>
{
    public string Name { get; set; } = null!;

    public ModelProviderProtocol Protocol { get; set; }

    /// <summary>名下模型的名称，按名称排序。</summary>
    public List<string> ModelNames { get; set; } = [];
}
