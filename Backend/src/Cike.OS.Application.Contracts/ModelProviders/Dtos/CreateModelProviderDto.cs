using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

public class CreateModelProviderDto
{
    public string Name { get; set; } = null!;

    public ModelProviderSetting Setting { get; set; } = null!;

    public List<SaveModelProviderMappingDto> Mappings { get; set; } = [];
}
