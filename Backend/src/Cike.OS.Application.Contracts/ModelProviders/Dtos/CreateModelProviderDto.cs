using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>模型提供方的新建输入：连接字段与它提供的全部模型。</summary>
public class CreateModelProviderDto
{
    public string Name { get; set; } = null!;

    public ModelProviderProtocol Protocol { get; set; }

    public string BaseUrl { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    /// <summary>协议专属设置；其判别键必须与 <see cref="Protocol"/> 一致。</summary>
    public ModelProviderSetting Setting { get; set; } = null!;

    /// <summary>提交的是期望的完整模型列表，没提交的模型会被移除。</summary>
    public List<SaveModelProviderModelDto> Models { get; set; } = [];
}
