using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>模型提供方的更新输入：Id 在路由；协议可改，所以也在请求体里。</summary>
public class UpdateModelProviderDto
{
    public string Name { get; set; } = null!;

    public ModelProviderProtocol Protocol { get; set; }

    public string BaseUrl { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    /// <summary>协议专属设置；其判别键必须与 <see cref="Protocol"/> 一致。</summary>
    public ModelProviderSetting Setting { get; set; } = null!;

    /// <summary>提交的是期望的完整模型列表：名称已存在的更新、新出现的名称新增、没提交的移除。</summary>
    public List<SaveModelProviderModelDto> Models { get; set; } = [];
}
