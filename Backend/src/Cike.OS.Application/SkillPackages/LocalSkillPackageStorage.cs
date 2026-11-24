using Cike.Core.DependencyInjection;
using Cike.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Compression;

namespace Cike.OS.Application.SkillPackages;

/// <summary>
/// 本地磁盘技能包存储：zip 解压到 <c>{RootPath}/{tenantId}/{guid}</c>，
/// 先解压到临时目录再整体移动，任一校验失败不留残留。
/// </summary>
public class LocalSkillPackageStorage : ISkillPackageStorage, ITransientDependency
{
    private readonly SkillStorageOptions _options;
    private readonly ILogger<LocalSkillPackageStorage> _logger;

    public LocalSkillPackageStorage(IOptions<SkillStorageOptions> options, ILogger<LocalSkillPackageStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> SaveAsync(Stream zipContent, long tenantId, CancellationToken cancellationToken = default)
    {
        var rootPath = Path.GetFullPath(_options.RootPath);
        Directory.CreateDirectory(rootPath);

        var directoryName = Guid.NewGuid().ToString("N");
        var finalDirectory = Path.Combine(rootPath, tenantId.ToString(), directoryName);
        var tempDirectory = Path.Combine(rootPath, tenantId.ToString(), directoryName + ".tmp");

        try
        {
            using var archive = OpenArchive(zipContent);
            ValidateArchive(archive);

            Directory.CreateDirectory(tempDirectory);
            await ExtractAsync(archive, tempDirectory, cancellationToken);

            Directory.CreateDirectory(Path.GetDirectoryName(finalDirectory)!);
            Directory.Move(tempDirectory, finalDirectory);
        }
        catch
        {
            TryDeleteDirectory(tempDirectory);
            throw;
        }

        return Path.Combine(_options.RootName, tenantId.ToString(), directoryName).Replace('\\', '/');
    }

    public Task DeleteAsync(string directory, long tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var physicalPath = ResolvePhysicalPath(directory, tenantId);
        if (physicalPath == null)
        {
            _logger.LogWarning("Skill package directory is not a valid path of tenant {TenantId} and will not be deleted: {Directory}", tenantId, directory);
            return Task.CompletedTask;
        }

        TryDeleteDirectory(physicalPath);
        return Task.CompletedTask;
    }

    private ZipArchive OpenArchive(Stream zipContent)
    {
        if (zipContent.CanSeek && zipContent.Length > _options.MaxUploadBytes)
        {
            throw new UserFriendlyException($"技能包超过大小上限（{_options.MaxUploadBytes / 1024 / 1024} MB）。");
        }

        try
        {
            return new ZipArchive(zipContent, ZipArchiveMode.Read, leaveOpen: false);
        }
        catch (InvalidDataException)
        {
            throw new UserFriendlyException("技能包不是有效的 zip 压缩包。");
        }
    }

    private void ValidateArchive(ZipArchive archive)
    {
        if (archive.Entries.Count == 0)
        {
            throw new UserFriendlyException("技能包为空。");
        }

        var totalExpanded = 0L;
        var hasEntryFile = false;

        foreach (var entry in archive.Entries)
        {
            totalExpanded += entry.Length;
            if (totalExpanded > _options.MaxExpandedBytes)
            {
                throw new UserFriendlyException($"技能包解压后超过大小上限（{_options.MaxExpandedBytes / 1024 / 1024} MB）。");
            }

            var rootEntryName = entry.FullName.Split('/')[0];
            if (string.Equals(rootEntryName, _options.EntryFileName, StringComparison.OrdinalIgnoreCase))
            {
                hasEntryFile = true;
            }
        }

        if (!hasEntryFile)
        {
            throw new UserFriendlyException($"技能包根目录必须包含 {_options.EntryFileName}。");
        }
    }

    private static async Task ExtractAsync(ZipArchive archive, string destination, CancellationToken cancellationToken)
    {
        var destinationRoot = Path.GetFullPath(destination)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // zip-slip 防护：规范化后的完整路径必须落在目标目录内
            var fullPath = Path.GetFullPath(Path.Combine(destination, entry.FullName));
            if (!fullPath.StartsWith(destinationRoot, StringComparison.Ordinal) && fullPath != destinationRoot.TrimEnd(Path.DirectorySeparatorChar))
            {
                throw new UserFriendlyException($"技能包包含非法路径：{entry.FullName}");
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(fullPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await using var source = entry.Open();
            await using var target = File.Create(fullPath);
            await source.CopyToAsync(target, cancellationToken);
        }
    }

    /// <summary>
    /// 把相对目录还原为物理路径；只接受 <c>{RootName}/{tenantId}/{guid}</c> 且租户段与 <paramref name="tenantId"/> 一致，
    /// 根目录、租户目录、跨租户、含 .. 或逃出根目录的一律返回 null（避免删除整根 / 整租户 / 跨租户）。
    /// </summary>
    private string? ResolvePhysicalPath(string directory, long tenantId)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        var normalized = directory.Replace('\\', '/').TrimStart('/');
        var prefix = _options.RootName.Replace('\\', '/').TrimStart('/').TrimEnd('/') + "/";
        if (!normalized.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var relative = normalized[prefix.Length..];
        var segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2 || relative.Contains("..", StringComparison.Ordinal) || segments[0] != tenantId.ToString())
        {
            return null;
        }

        var rootPath = Path.GetFullPath(_options.RootPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var physicalPath = Path.GetFullPath(Path.Combine(rootPath, relative));
        return physicalPath.StartsWith(rootPath, StringComparison.Ordinal) ? physicalPath : null;
    }

    private void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to delete skill package directory: {Path}", path);
        }
    }
}
