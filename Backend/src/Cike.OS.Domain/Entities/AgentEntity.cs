namespace Cike.OS.Domain.Entities;

/// <summary>
/// Agent（租户级聚合根）：名称、描述、指令与模型选项（默认值），以及它绑定的工具（<see cref="Tools"/>，聚合内子实体）。
/// 不绑定模型——模型在运行时选择。创建走 <see cref="Create"/>；
/// 非空 / 长度 / 范围等单字段规则、引用的工具包 / 工具是否存在一律在 FluentValidation 与 Handler，这里只断言同一条工具绑定只出现一次。
/// </summary>
public class AgentEntity : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Name { get; private set; } = null!;

    /// <summary>描述：可选，未填写为空字符串。</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>指令：Agent 的自我介绍与提示词，没有长度上限。</summary>
    public string Instructions { get; private set; } = null!;

    /// <summary>模型选项（默认值，运行时可调整）；json 列，更新时整体替换。</summary>
    public AgentOptions Options { get; private set; } = new();

    public List<AgentTool> Tools { get; private set; } = [];

    private AgentEntity()
    {
    }

    public static AgentEntity Create(string name, string? description, string instructions, AgentOptions options, IEnumerable<AgentTool> tools)
    {
        var agent = new AgentEntity
        {
            Name = name,
            Description = description ?? string.Empty,
            Instructions = instructions,
            Options = options,
        };
        agent.SyncTools(tools);
        return agent;
    }

    /// <summary>修改全部字段（没有不可变字段）；工具不在这里改，走 <see cref="SyncTools"/>。</summary>
    public void Update(string name, string? description, string instructions, AgentOptions options)
    {
        Name = name;
        Description = description ?? string.Empty;
        Instructions = instructions;
        Options = options;
    }

    /// <summary>
    /// 按提交的工具条目整体同步（以类型 + 目标 Id 为键）：已存在的保留，新出现的追加，未提交的移除。
    /// 同一条绑定只能出现一次是聚合内部不变量，输入里重复的在这里被拒。
    /// </summary>
    public void SyncTools(IEnumerable<AgentTool> tools)
    {
        var submitted = new List<AgentTool>();
        foreach (var tool in tools)
        {
            if (submitted.Any(x => x.Type == tool.Type && x.TargetId == tool.TargetId))
            {
                throw new UserFriendlyException($"工具绑定「{tool.Type} / {tool.TargetId}」在同一个 Agent 上只能出现一次。");
            }

            submitted.Add(tool);
        }

        // 从集合移除后，已加载跟踪的条目在保存时一并软删
        foreach (var tool in Tools.Where(x => !submitted.Any(s => s.Type == x.Type && s.TargetId == x.TargetId)).ToList())
        {
            Tools.Remove(tool);
        }

        foreach (var tool in submitted)
        {
            if (!Tools.Any(x => x.Type == tool.Type && x.TargetId == tool.TargetId))
            {
                Tools.Add(tool);
            }
        }
    }
}
