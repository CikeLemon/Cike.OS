using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.MultiTenancy;
using Cike.OS.Application.SkillPackages;
using Cike.OS.Application.Tools.Commands;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Cike.OS.Application.Tools;

public class ToolCommandHandler(
    IRepository<Tool, long> toolRepository,
    IRepository<UserToolConfiguration, long> userToolConfigurationRepository,
    IReadOnlyRepository<ToolPackage, long> toolPackageRepository,
    IReadOnlyRepository<AgentTool, long> agentToolRepository,
    ISkillPackageStorage skillPackageStorage,
    ICurrentTenant currentTenant,
    ILogger<ToolCommandHandler> logger)
{
    [LocalEventHandler]
    public async Task CreateAsync(CreateToolCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;

        _ = await toolPackageRepository.GetAsync(dto.ToolPackageId, cancellationToken);
        await EnsureCodeUniqueAsync(dto.Code, null, cancellationToken);

        var tool = Tool.Create(dto.Code, dto.Name, dto.Description, dto.Type, dto.ToolPackageId, dto.Parameters, dto.Setting);

        await toolRepository.InsertAsync(tool, cancellationToken: cancellationToken);
        command.Id = tool.Id;
    }

    [LocalEventHandler]
    public async Task UpdateAsync(UpdateToolCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        var tool = await toolRepository.GetAsync(command.Id, cancellationToken);

        if (dto.ToolPackageId != tool.ToolPackageId)
        {
            _ = await toolPackageRepository.GetAsync(dto.ToolPackageId, cancellationToken);
        }

        await EnsureCodeUniqueAsync(dto.Code, tool.Id, cancellationToken);

        // 技能工具换了技能包目录 → 记下待清理的旧目录（非技能设置会在 Update 里被「形态不可变」拒绝）
        var replacedSkillDirectory = tool.IsSkill && tool.SkillPackageDirectory != (dto.Setting as SkillToolSetting)?.Directory
            ? tool.SkillPackageDirectory
            : null;

        tool.Update(dto.Code, dto.Name, dto.Description, dto.ToolPackageId, dto.Parameters, dto.Setting);

        await toolRepository.UpdateAsync(tool, cancellationToken: cancellationToken);

        // 先持久化再删文件：提交失败时旧包还在
        if (replacedSkillDirectory is not null)
        {
            await DeleteSkillDirectoryAsync(replacedSkillDirectory, cancellationToken);
        }
    }

    [LocalEventHandler]
    public async Task DeleteAsync(DeleteToolCommand command, CancellationToken cancellationToken = default)
    {
        var tool = await toolRepository.GetAsync(command.Id, cancellationToken);

        // 只看单个工具的绑定行；整包绑定不计（包里少一个工具只是不生效，不需要守护）
        var agentToolCount = await agentToolRepository.GetCountAsync(x => x.Type == AgentToolType.Tool && x.TargetId == tool.Id, cancellationToken);
        if (agentToolCount > 0)
        {
            throw new UserFriendlyException("该工具已被 Agent 绑定引用，请先解除绑定后再删除。");
        }

        var configurations = await userToolConfigurationRepository.GetListAsync(x => x.ToolId == tool.Id, cancellationToken: cancellationToken);
        if (configurations.Count > 0)
        {
            await userToolConfigurationRepository.DeleteManyAsync(configurations, cancellationToken: cancellationToken);
        }

        await toolRepository.DeleteAsync(tool, cancellationToken: cancellationToken);

        // 技能工具删除后，它独占的技能包目录一并清理
        if (tool.IsSkill && tool.SkillPackageDirectory is not null)
        {
            await DeleteSkillDirectoryAsync(tool.SkillPackageDirectory, cancellationToken);
        }
    }

    private async Task EnsureCodeUniqueAsync(string code, long? excludeId, CancellationToken cancellationToken)
    {
        var normalized = code.Trim();
        var count = await toolRepository.GetCountAsync(x => x.Code == normalized && x.Id != excludeId, cancellationToken);
        if (count > 0)
        {
            throw new UserFriendlyException($"工具编码「{normalized}」已存在。");
        }
    }

    /// <summary>删除技能包目录；存储层只接受归属当前租户的路径，失败只记日志。</summary>
    private async Task DeleteSkillDirectoryAsync(string directory, CancellationToken cancellationToken)
    {
        try
        {
            await skillPackageStorage.DeleteAsync(directory, currentTenant.Id, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to delete skill package directory after tool change: {Directory}", directory);
        }
    }
}
