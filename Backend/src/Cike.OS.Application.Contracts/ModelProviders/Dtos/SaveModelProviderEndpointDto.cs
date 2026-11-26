using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>
/// 提供商映射之外，接入点本身的输入：随提供方整体提交，以 Id 为键（已有的更新、Id 为空的新增、未提交的移除）。
/// 协议只在新建时生效，已有的接入点提交时必须带上现有协议。
/// </summary>
public class SaveModelProviderEndpointDto
{
    /// <summary>为空（0）表示新建。</summary>
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public ModelProviderProtocol Protocol { get; set; }

    public string BaseUrl { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    /// <summary>协议专属设置；其判别键必须与 <see cref="Protocol"/> 一致。</summary>
    public ModelProviderEndpointSetting Setting { get; set; } = null!;

    public List<SaveModelProviderMappingDto> Mappings { get; set; } = [];
}
