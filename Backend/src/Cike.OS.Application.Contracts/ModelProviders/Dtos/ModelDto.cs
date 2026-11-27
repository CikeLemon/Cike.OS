using Cike.Contracts.EntityDtos;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>模型提供方下的一个模型（聚合内子实体，随 <see cref="ModelProviderDto"/> 一起带出）。</summary>
public class ModelDto : AuditedEntityDto<long>
{
    /// <summary>模型名称：调用时原样发给提供方，创建后不可变。</summary>
    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public List<long> TagIds { get; set; } = [];
}
