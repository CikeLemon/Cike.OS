using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Domain.Entities;

/// <summary>
/// 模型提供方（租户级聚合根）：一条模型连接——名称、一种协议、接入地址、密钥、协议专属设置，
/// 以及它提供的模型（<see cref="Models"/>，聚合内子实体）。同一家供应商有几种协议就建几个提供方。
/// 创建走 <see cref="Create"/>；协议可以修改（删掉提供方重建会丢掉名下全部模型）。
/// 非空 / 长度等单字段规则、设置子类型与协议是否匹配一律在 FluentValidation，这里只断言同一模型名只出现一次。
/// </summary>
public class ModelProvider : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Name { get; private set; } = null!;

    public ModelProviderProtocol Protocol { get; private set; }

    public string BaseUrl { get; private set; } = null!;

    public string ApiKey { get; private set; } = null!;

    /// <summary>协议专属设置（多态值对象）；接入地址与密钥不在这里。</summary>
    public ModelProviderSetting Setting { get; private set; } = null!;

    public List<ModelEntity> Models { get; private set; } = [];

    private ModelProvider()
    {
    }

    public static ModelProvider Create(
        string name,
        ModelProviderProtocol protocol,
        string baseUrl,
        string apiKey,
        ModelProviderSetting setting,
        IEnumerable<ModelEntity> models)
    {
        var provider = new ModelProvider
        {
            Name = name,
            Protocol = protocol,
            BaseUrl = baseUrl,
            ApiKey = apiKey,
            Setting = setting,
        };
        provider.SyncModels(models);
        return provider;
    }

    /// <summary>修改连接信息（名称、协议、接入地址、密钥、协议专属设置）；名下的模型不受影响。</summary>
    public void Update(string name, ModelProviderProtocol protocol, string baseUrl, string apiKey, ModelProviderSetting setting)
    {
        Name = name;
        Protocol = protocol;
        BaseUrl = baseUrl;
        ApiKey = apiKey;
        Setting = setting;
    }

    /// <summary>
    /// 按提交的模型整体同步（以名称为键）：名称已存在的更新描述与标签，新出现的名称新增，未提交的移除。
    /// 名称是键，所以创建后不可变；同一提供方下名称不重复是聚合内部不变量，输入里重复的名称在这里被拒。
    /// </summary>
    public void SyncModels(IEnumerable<ModelEntity> models)
    {
        var submitted = new List<ModelEntity>();
        foreach (var model in models)
        {
            if (submitted.Any(x => x.Name == model.Name))
            {
                throw new UserFriendlyException($"模型「{model.Name}」在本提供方下只能添加一次。");
            }

            submitted.Add(model);
        }

        var names = submitted.Select(x => x.Name).ToHashSet();

        // 从集合移除后，已加载跟踪的模型在保存时一并软删
        foreach (var model in Models.Where(x => !names.Contains(x.Name)).ToList())
        {
            Models.Remove(model);
        }

        foreach (var model in submitted)
        {
            var existing = Models.FirstOrDefault(x => x.Name == model.Name);
            if (existing == null)
            {
                Models.Add(model);
            }
            else
            {
                existing.Update(model.Description, model.TagIds);
            }
        }
    }
}
