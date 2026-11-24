namespace Cike.OS.Application.SkillPackages;

/// <summary>
/// 技能包存储选项，绑定配置节 <see cref="SectionName"/>。
/// </summary>
public class SkillStorageOptions
{
    public const string SectionName = "SkillStorage";

    /// <summary>技能包物理存储根目录（相对于进程工作目录或绝对路径）。</summary>
    public string RootPath { get; set; } = "data/skills";

    /// <summary>相对路径的首段，<see cref="ISkillPackageStorage"/> 返回的目录以此开头。</summary>
    public string RootName { get; set; } = "skills";

    /// <summary>上传 zip 的最大字节数。</summary>
    public long MaxUploadBytes { get; set; } = 50L * 1024 * 1024;

    /// <summary>解压后允许的最大总字节数，防止压缩炸弹。</summary>
    public long MaxExpandedBytes { get; set; } = 500L * 1024 * 1024;

    /// <summary>技能包根目录必须存在的入口文件（不区分大小写）。</summary>
    public string EntryFileName { get; set; } = "SKILL.md";
}
