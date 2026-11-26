namespace Cike.OS.Domain.Entities;

/// <summary>
/// 接入点（模型提供方聚合内的子实体）：提供方的一种连接入口——用哪种协议、哪个接入地址、哪把密钥，
/// 以及它接入了哪些模型（<see cref="Mappings"/>，接入点下的子实体）。
/// 只能经 <see cref="ModelProvider"/> 的意图方法创建 / 修改，没有独立的聚合根。
/// 非空 / 长度等单字段规则一律在 FluentValidation，这里只断言设置子类型与协议匹配。
/// </summary>
public class ModelProviderEndpoint : FullAuditedEntity<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public long ModelProviderId { get; private set; }

    public string Name { get; private set; } = null!;

    /// <summary>协议：创建后不可变，要换协议只能删掉接入点重建。</summary>
    public ModelProviderProtocol Protocol { get; private set; }

    public string BaseUrl { get; private set; } = null!;

    public string ApiKey { get; private set; } = null!;

    /// <summary>协议专属设置（多态值对象）；接入地址与密钥不在这里。</summary>
    public ModelProviderEndpointSetting Setting { get; private set; } = null!;

    public List<ModelProviderMapping> Mappings { get; private set; } = [];

    private ModelProviderEndpoint()
    {
    }

    // 只允许聚合根创建与修改子实体，Handler 不直接碰
    internal static ModelProviderEndpoint Create(
        ModelProviderProtocol protocol,
        string name,
        string baseUrl,
        string apiKey,
        ModelProviderEndpointSetting setting)
    {
        AssertSettingMatchesProtocol(protocol, setting);

        return new ModelProviderEndpoint
        {
            Protocol = protocol,
            Name = name,
            BaseUrl = baseUrl,
            ApiKey = apiKey,
            Setting = setting,
        };
    }

    internal void Update(string name, string baseUrl, string apiKey, ModelProviderEndpointSetting setting)
    {
        AssertSettingMatchesProtocol(Protocol, setting);

        Name = name;
        BaseUrl = baseUrl;
        ApiKey = apiKey;
        Setting = setting;
    }

    /// <summary>
    /// 按提交的映射整体同步（以模型为键）：已映射的模型更新分数与提供商侧模型名，新出现的模型新增，未提交的移除。
    /// 一个接入点对一个模型只保留一条映射是聚合内部不变量，输入里重复的模型在这里被拒。
    /// </summary>
    internal void SyncMappings(IEnumerable<(long ModelId, int Priority, string? ProviderModelName)> mappings)
    {
        var submitted = new List<(long ModelId, int Priority, string? ProviderModelName)>();
        foreach (var mapping in mappings)
        {
            if (submitted.Any(x => x.ModelId == mapping.ModelId))
            {
                throw new UserFriendlyException("同一个模型在接入点下只能映射一次。");
            }

            submitted.Add(mapping);
        }

        var modelIds = submitted.Select(x => x.ModelId).ToHashSet();

        foreach (var mapping in Mappings.Where(x => !modelIds.Contains(x.ModelId)).ToList())
        {
            Mappings.Remove(mapping);
        }

        foreach (var mapping in submitted)
        {
            var existing = Mappings.FirstOrDefault(x => x.ModelId == mapping.ModelId);
            if (existing == null)
            {
                Mappings.Add(ModelProviderMapping.Create(mapping.ModelId, mapping.Priority, mapping.ProviderModelName));
            }
            else
            {
                existing.Update(mapping.Priority, mapping.ProviderModelName);
            }
        }
    }

    /// <summary>聚合内部不变量：协议专属设置的子类型必须与协议一一对应。</summary>
    private static void AssertSettingMatchesProtocol(ModelProviderProtocol protocol, ModelProviderEndpointSetting? setting)
    {
        var matches = (protocol, setting) switch
        {
            (ModelProviderProtocol.OpenAI, OpenAIModelProviderEndpointSetting) => true,
            (ModelProviderProtocol.Anthropic, AnthropicModelProviderEndpointSetting) => true,
            _ => false,
        };

        if (!matches)
        {
            throw new UserFriendlyException("协议与设置类型不匹配。");
        }
    }
}
