using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

public class ModelProviderDto : AuditedEntityDto<long>
{
    public string Name { get; set; } = null!;

    public ModelProviderSetting Setting { get; set; } = null!;

    public List<ModelProviderMappingDto> Mappings { get; set; } = [];
}
