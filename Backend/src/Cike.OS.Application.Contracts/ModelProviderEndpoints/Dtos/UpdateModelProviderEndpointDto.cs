using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;

/// <summary>接入点的更新输入：连接信息、协议专属设置，以及它的全部模型映射。所属提供方与协议创建后都不可变，不在更新之列。</summary>
public class UpdateModelProviderEndpointDto
{
    public string Name { get; set; } = null!;

    public string BaseUrl { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    /// <summary>协议专属设置；其判别键必须与该接入点现有的协议一致。</summary>
    public ModelProviderEndpointSetting Setting { get; set; } = null!;

    /// <summary>提交的是期望的完整状态：没提交的映射会被移除。</summary>
    public List<SaveModelProviderMappingDto> Mappings { get; set; } = [];
}
