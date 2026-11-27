namespace Cike.OS.Application.Contracts.ModelProviders.Dtos;

/// <summary>
/// 模型提供方的更新输入：只有名称。接入点有它自己的接口，不随提供方提交。
/// </summary>
public class UpdateModelProviderDto
{
    public string Name { get; set; } = null!;
}
