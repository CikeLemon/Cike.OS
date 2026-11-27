using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>接入点的新建输入：协议、连接信息、协议专属设置，以及它的全部模型映射。</summary>
public class CreateModelProviderEndpointDto
{
    public string Name { get; set; } = null!;

    /// <summary>协议：创建后不可变，要换协议只能删掉接入点重建。</summary>
    public ModelProviderProtocol Protocol { get; set; }

    public string BaseUrl { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    /// <summary>协议专属设置；其判别键必须与 <see cref="Protocol"/> 一致。</summary>
    public ModelProviderEndpointSetting Setting { get; set; } = null!;

    public List<SaveModelProviderMappingDto> Mappings { get; set; } = [];
}
