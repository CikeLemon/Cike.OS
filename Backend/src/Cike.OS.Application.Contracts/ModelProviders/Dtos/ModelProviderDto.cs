using Cike.Contracts.EntityDtos;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>模型提供方：一个供应商在本租户下的条目，本身只有名称；连接与映射都在它的接入点上，接入点经它自己的列表接口读取。</summary>
public class ModelProviderDto : AuditedEntityDto<long>
{
    public string Name { get; set; } = null!;
}
