using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Domain.Entities;

/// <summary>
/// 模型提供方（租户级聚合根）：一个供应商在本租户下的条目，本身只有名称。
/// 连接与模型映射都在它的接入点（<see cref="Endpoints"/>，聚合内子实体）上；接入点只能经本聚合的意图方法添加 / 修改 / 移除。
/// </summary>
public class ModelProvider : FullAuditedAggregateRoot<long>, IMultiTenant
{
    public long TenantId { get; set; }

    public string Name { get; private set; } = null!;

    public List<ModelProviderEndpoint> Endpoints { get; private set; } = [];

    private ModelProvider()
    {
    }

    private ModelProvider(string name)
    {
        Name = name;
    }

    public static ModelProvider Create(string name)
    {
        return new ModelProvider(name);
    }

    public void Rename(string name)
    {
        Name = name;
    }

    /// <summary>添加一个接入点并同步它的全部映射；接入点随本聚合保存后才拿到 Id。</summary>
    public ModelProviderEndpoint AddEndpoint(
        ModelProviderProtocol protocol,
        string name,
        string baseUrl,
        string apiKey,
        ModelProviderEndpointSetting setting,
        IEnumerable<ModelProviderEndpoint.MappingInput> mappings)
    {
        AssertEndpointNameAvailable(name, null);

        var endpoint = ModelProviderEndpoint.Create(protocol, name, baseUrl, apiKey, setting);
        endpoint.SyncMappings(mappings);
        Endpoints.Add(endpoint);
        return endpoint;
    }

    /// <summary>修改一个接入点（协议创建后不可变，不在修改之列）并同步它的全部映射。</summary>
    public void UpdateEndpoint(
        long endpointId,
        string name,
        string baseUrl,
        string apiKey,
        ModelProviderEndpointSetting setting,
        IEnumerable<ModelProviderEndpoint.MappingInput> mappings)
    {
        var endpoint = FindEndpoint(endpointId);
        AssertEndpointNameAvailable(name, endpointId);

        endpoint.Update(name, baseUrl, apiKey, setting);
        endpoint.SyncMappings(mappings);
    }

    /// <summary>移除一个接入点：它已处于跟踪状态，随本聚合保存时连同映射一并软删。</summary>
    public void RemoveEndpoint(long endpointId)
    {
        Endpoints.Remove(FindEndpoint(endpointId));
    }

    /// <summary>聚合内部不变量：接入点名称在提供方内互不重复（对着已加载的集合断言，修改时排除自己）。</summary>
    private void AssertEndpointNameAvailable(string name, long? endpointId)
    {
        if (Endpoints.Any(x => x.Id != endpointId && x.Name == name))
        {
            throw new UserFriendlyException($"接入点名称「{name}」已存在。");
        }
    }

    /// <summary>带 Id 的必须是名下已有的接入点：不能借修改把别人的接入点挂进来。</summary>
    private ModelProviderEndpoint FindEndpoint(long endpointId)
    {
        return Endpoints.FirstOrDefault(x => x.Id == endpointId)
            ?? throw new UserFriendlyException("接入点不存在。");
    }
}
