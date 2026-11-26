namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>
/// 模型提供方的更新输入：名称 + 名下全部接入点（每个接入点带它的全部映射）。
/// 提交的是期望的完整状态：没提交的接入点会被删除。
/// </summary>
public class UpdateModelProviderDto
{
    public string Name { get; set; } = null!;

    public List<SaveModelProviderEndpointDto> Endpoints { get; set; } = [];
}
