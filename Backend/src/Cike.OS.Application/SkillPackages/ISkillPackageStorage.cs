namespace Cike.OS.Application.SkillPackages;

/// <summary>
/// 技能包存储抽象：目录以「相对存储根的路径」标识，领域层只持有该路径，不感知物理位置。
/// </summary>
public interface ISkillPackageStorage
{
    /// <summary>
    /// 保存一个技能包 zip：校验并解压到新目录，返回相对路径。
    /// </summary>
    Task<string> SaveAsync(Stream zipContent, long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除归属 <paramref name="tenantId"/> 的技能包目录；目录不属于该租户、格式不合法或不存在时静默返回。
    /// </summary>
    Task DeleteAsync(string directory, long tenantId, CancellationToken cancellationToken = default);
}
