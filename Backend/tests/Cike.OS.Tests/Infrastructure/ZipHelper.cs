using System.IO.Compression;
using System.Text;

namespace Cike.OS.Tests.Infrastructure;

/// <summary>在内存里构造技能包 zip，供上传用例使用。</summary>
public static class ZipHelper
{
    public static byte[] Create(params (string EntryName, string Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (entryName, content) in entries)
            {
                var entry = archive.CreateEntry(entryName);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write(content);
            }
        }

        return stream.ToArray();
    }

    public static byte[] CreateSkillPackage(string skillName = "demo")
    {
        return Create(
            ("SKILL.md", $"---\nname: {skillName}\n---\n# {skillName}\n"),
            ("scripts/run.py", "print('hello')\n"));
    }

    /// <summary>不可压缩的随机内容（base64 文本），用于撑大 zip 体积测大小上限。</summary>
    public static string RandomText(int byteCount)
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToBase64String(bytes);
    }
}
