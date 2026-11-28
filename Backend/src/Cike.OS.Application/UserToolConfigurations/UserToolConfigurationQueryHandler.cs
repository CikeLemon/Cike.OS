using Cike.Contracts.EntityDtos;
using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;
using Cike.OS.Application.UserToolConfigurations.Queries;
using Cike.OS.Domain.Entities;
using Cike.Security;
using Mapster;

namespace Cike.OS.Application.UserToolConfigurations;

public class UserToolConfigurationQueryHandler(
    IReadOnlyRepository<UserToolConfiguration, long> userToolConfigurationRepository,
    IReadOnlyRepository<Tool, long> toolRepository,
    IReadOnlyRepository<ToolPackage, long> toolPackageRepository,
    IReadOnlyRepository<ToolPackageCategory, long> toolPackageCategoryRepository,
    ICurrentUser currentUser)
{
    private string RequireUserId() => currentUser.Id
        ?? throw new UserFriendlyException("Current user id is required.");

    /// <summary>按工具 Id 取当前用户的配置详情：编辑弹窗按工具此刻的参数定义生成字段、用已存的值回填。没有配置或工具已不存在时返回 null。</summary>
    [LocalEventHandler]
    public async Task GetDetailAsync(GetUserToolConfigurationDetailQuery query, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        using var _ = userToolConfigurationRepository.BeginAsNoTracking();
        using var __ = toolRepository.BeginAsNoTracking();

        var configurations = await userToolConfigurationRepository.ToListAsync(
            userToolConfigurationRepository.GetQueryable().Where(x => x.ToolId == query.ToolId && x.UserId == userId),
            cancellationToken);
        var configuration = configurations.FirstOrDefault();
        if (configuration == null)
        {
            query.Result = null;
            return;
        }

        // 已删工具的残留配置与卡片列表的内连接口径一致：视为不存在
        var tool = await toolRepository.FindAsync(query.ToolId, cancellationToken);
        if (tool == null)
        {
            query.Result = null;
            return;
        }

        var dto = configuration.Adapt<UserToolConfigurationDetailDto>();
        dto.ToolName = tool.Name;
        dto.Parameters = tool.Parameters;
        query.Result = dto;
    }

    /// <summary>
    /// 工具参数页的卡片列表：当前用户的配置 LINQ 联合工具表后分页（内连接天然隐藏已删工具的残留配置，关键字也按工具名在这里过滤），
    /// 全程不跟踪；本页用到的工具 / 工具包 / 分类信息按页内工具 Id 各一次批量取回，实体 → 列表 DTO 交给 Mapster，跨表字段在 Adapt 之后回填。
    /// </summary>
    [LocalEventHandler]
    public async Task GetPagedListAsync(GetUserToolConfigurationPagedListQuery query, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        using var _ = userToolConfigurationRepository.BeginAsNoTracking();
        using var __ = toolRepository.BeginAsNoTracking();
        using var ___ = toolPackageRepository.BeginAsNoTracking();
        using var ____ = toolPackageCategoryRepository.BeginAsNoTracking();

        var rows =
            from configuration in userToolConfigurationRepository.GetQueryable().Where(x => x.UserId == userId)
            join tool in toolRepository.GetQueryable() on configuration.ToolId equals tool.Id
            select new { configuration, tool.Name };
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            rows = rows.Where(x => x.Name.Contains(query.Keyword));
        }

        // Paging 从端点 [AsParameters] 绑定后一路透传：Sorting 由前端传入（作用于配置实体的列）
        var (total, page) = await userToolConfigurationRepository.ToPagedListAsync(rows.Select(x => x.configuration), query.Paging, cancellationToken);

        var toolIds = page.Select(x => x.ToolId).Distinct().ToList();
        var tools = await toolRepository.ToListAsync(
            toolRepository.GetQueryable()
                .Where(x => toolIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name, x.Type, x.ToolPackageId, x.Parameters }),
            cancellationToken);
        var packageIds = tools.Select(x => x.ToolPackageId).Distinct().ToList();
        var packages = await toolPackageRepository.ToListAsync(
            toolPackageRepository.GetQueryable()
                .Where(x => packageIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name, x.ToolPackageCategoryId }),
            cancellationToken);
        var categoryIds = packages.Select(x => x.ToolPackageCategoryId).Distinct().ToList();
        var categories = await toolPackageCategoryRepository.ToListAsync(
            toolPackageCategoryRepository.GetQueryable()
                .Where(x => categoryIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Name }),
            cancellationToken);

        var toolById = tools.ToDictionary(x => x.Id);
        var packageById = packages.ToDictionary(x => x.Id);
        var categoryNameById = categories.ToDictionary(x => x.Id, x => x.Name);

        var items = page.Adapt<List<UserToolConfigurationItemDto>>();
        foreach (var item in items)
        {
            var tool = toolById[item.ToolId];
            var package = packageById[tool.ToolPackageId];
            item.ToolName = tool.Name;
            item.ToolType = tool.Type;
            item.ToolPackageName = package.Name;
            item.ToolPackageCategoryName = categoryNameById[package.ToolPackageCategoryId];
            item.Parameters = tool.Parameters;
        }

        query.Result = new PagedResultDto<UserToolConfigurationItemDto>(total, items);
    }

    /// <summary>批量新增弹窗的候选：全部声明了参数的工具（参数定义是 json 列，不能下推 Count，取回后在内存过滤）+ 当前用户是否已配置。</summary>
    [LocalEventHandler]
    public async Task GetToolOptionsAsync(GetUserToolConfigurationToolOptionsQuery query, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        using var _ = toolRepository.BeginAsNoTracking();
        using var __ = toolPackageRepository.BeginAsNoTracking();
        using var ___ = userToolConfigurationRepository.BeginAsNoTracking();

        var tools = await toolRepository.ToListAsync(
            toolRepository.GetQueryable()
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    ToolId = x.Id,
                    ToolName = x.Name,
                    ToolType = x.Type,
                    x.ToolPackageId,
                    x.Parameters,
                }),
            cancellationToken);
        var withParameters = tools.Where(x => x.Parameters.Count > 0).ToList();
        var toolIds = withParameters.Select(x => x.ToolId).ToList();
        var packageIds = withParameters.Select(x => x.ToolPackageId).Distinct().ToList();

        var packageNames = await toolPackageRepository.ToListAsync(
            toolPackageRepository.GetQueryable()
                .Where(x => packageIds.Contains(x.Id))
                .Select(x => new { PackageId = x.Id, x.Name }),
            cancellationToken);
        var packageNameById = packageNames.ToDictionary(x => x.PackageId, x => x.Name);

        var configuredToolIds = (await userToolConfigurationRepository.ToListAsync(
            userToolConfigurationRepository.GetQueryable()
                .Where(x => x.UserId == userId && toolIds.Contains(x.ToolId))
                .Select(x => x.ToolId),
            cancellationToken)).ToHashSet();

        query.Result = withParameters.Select(tool =>
        {
            var option = tool.Adapt<UserToolConfigurationToolOptionItemDto>();
            option.ToolPackageName = packageNameById[tool.ToolPackageId];
            if (configuredToolIds.Contains(tool.ToolId)) option.Configured = true;

            return option;
        }).ToList();
    }
}
