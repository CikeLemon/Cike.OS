using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>提供商类型创建后不可变，设置子类型必须与现有类型一致，由 Handler 校验。</summary>
public class UpdateModelProviderDto
{
    public string Name { get; set; } = null!;

    public ModelProviderSetting Setting { get; set; } = null!;

    public List<SaveModelProviderMappingDto> Mappings { get; set; } = [];
}
