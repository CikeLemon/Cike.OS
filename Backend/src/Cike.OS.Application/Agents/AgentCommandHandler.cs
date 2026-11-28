using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Agents.Commands;
using Cike.OS.Application.Contracts.Agents.Dtos;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Application.Agents;

/// <summary>
/// Agent 用例：Agent 工具是聚合内的子实体、随 Agent 的请求整体同步，没有独立用例。
/// 更新 / 删除前一律连同工具加载：条目要处于跟踪状态，被移除后才会跟着软删。
/// </summary>
public class AgentCommandHandler(
    IRepository<AgentEntity, long> agentRepository,
    IReadOnlyRepository<ToolPackage, long> toolPackageRepository,
    IReadOnlyRepository<Tool, long> toolRepository)
{
    [LocalEventHandler]
    public async Task CreateAsync(CreateAgentCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        await ValidateAsync(dto.Name, dto.Tools, null, cancellationToken);

        var agent = AgentEntity.Create(dto.Name, dto.Description, dto.Instructions, dto.Options, ToTools(dto.Tools));
        await agentRepository.InsertAsync(agent, cancellationToken: cancellationToken);
        command.Id = agent.Id;
    }

    [LocalEventHandler]
    public async Task UpdateAsync(UpdateAgentCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;
        var agent = await FindWithToolsAsync(command.Id, cancellationToken)
            ?? throw new UserFriendlyException("Agent 不存在。");
        await ValidateAsync(dto.Name, dto.Tools, agent.Id, cancellationToken);

        agent.Update(dto.Name, dto.Description, dto.Instructions, dto.Options);
        agent.SyncTools(ToTools(dto.Tools));
        await agentRepository.UpdateAsync(agent, cancellationToken: cancellationToken);
    }

    [LocalEventHandler]
    public async Task DeleteAsync(DeleteAgentCommand command, CancellationToken cancellationToken = default)
    {
        // 名下的工具条目随聚合一并软删，没有任何守护（会话仍可查看，只是不能再对话）
        var agent = await FindWithToolsAsync(command.Id, cancellationToken)
            ?? throw new UserFriendlyException("Agent 不存在。");

        await agentRepository.DeleteAsync(agent, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 创建 / 更新共用的数据校验：名称同租户内唯一（excludeId 排除自身）；整包绑定指向的工具包、单选指向的工具必须都存在；
    /// 同一工具包不能既整包绑定又单选其中的工具。软删过滤生效，并发窗口按 ADR 0001 接受。
    /// </summary>
    private async Task ValidateAsync(string name, List<SaveAgentToolDto> tools, long? excludeId, CancellationToken cancellationToken)
    {
        if (await agentRepository.GetCountAsync(x => x.Name == name && x.Id != excludeId, cancellationToken) > 0)
        {
            throw new UserFriendlyException($"Agent「{name}」已存在。");
        }

        var packageIds = tools.Where(x => x.Type == AgentToolType.ToolPackage).Select(x => x.TargetId).Distinct().ToList();
        if (packageIds.Count > 0
            && await toolPackageRepository.GetCountAsync(x => packageIds.Contains(x.Id), cancellationToken) != packageIds.Count)
        {
            throw new UserFriendlyException("部分工具包不存在，请刷新后重新选择。");
        }

        var toolIds = tools.Where(x => x.Type == AgentToolType.Tool).Select(x => x.TargetId).Distinct().ToList();
        if (toolIds.Count == 0)
        {
            return;
        }

        if (await toolRepository.GetCountAsync(x => toolIds.Contains(x.Id), cancellationToken) != toolIds.Count)
        {
            throw new UserFriendlyException("部分工具不存在，请刷新后重新选择。");
        }

        if (packageIds.Count == 0)
        {
            return;
        }

        // 工具属于哪个包在另一张表：一次投影取回后在内存比对
        using var _ = toolRepository.BeginAsNoTracking();
        var packageIdsOfTools = await toolRepository.ToListAsync(
            toolRepository.GetQueryable()
                .Where(x => toolIds.Contains(x.Id))
                .Select(x => x.ToolPackageId),
            cancellationToken);
        if (packageIdsOfTools.Any(packageIds.Contains))
        {
            throw new UserFriendlyException("工具包已整包绑定，不能再单独绑定其中的工具。");
        }
    }

    /// <summary>连同工具加载一个 Agent：工具是这个聚合里唯一的一级导航，通用仓储的预加载就够。</summary>
    private async Task<AgentEntity?> FindWithToolsAsync(long id, CancellationToken cancellationToken)
    {
        var agents = await agentRepository.ToListAsync(
            agentRepository.WithDetails(x => x.Tools).Where(x => x.Id == id), cancellationToken);
        return agents.FirstOrDefault();
    }

    private static IEnumerable<AgentTool> ToTools(List<SaveAgentToolDto> tools) =>
        tools.Select(x => AgentTool.Create(x.Type, x.TargetId));
}
