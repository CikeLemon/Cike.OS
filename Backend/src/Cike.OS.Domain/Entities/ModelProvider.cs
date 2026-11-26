using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;

namespace Cike.OS.Domain.Entities;

/// <summary>
/// 模型提供方（租户级聚合根）：一个供应商在本租户下的条目，本身只有名称。
/// 连接与模型映射都在它的接入点（<see cref="Endpoints"/>，聚合内子实体）上；接入点随本聚合整体同步，没有独立的聚合根与对外接口。
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

    /// <summary>提交给 <see cref="SyncEndpoints"/> 的一个接入点；<see cref="Id"/> 为 0 表示新建。</summary>
    public readonly record struct EndpointInput(
        long Id,
        ModelProviderProtocol Protocol,
        string Name,
        string BaseUrl,
        string ApiKey,
        ModelProviderEndpointSetting Setting,
        IReadOnlyList<(long ModelId, int Priority, string? ProviderModelName)> Mappings);

    /// <summary>
    /// 按提交的接入点整体同步（以 Id 为键）：名下已有的更新，Id 为 0 的新增，未提交的移除；
    /// 每个接入点的映射随它整体同步。
    /// </summary>
    public void SyncEndpoints(IEnumerable<EndpointInput> endpoints)
    {
        var submitted = endpoints.ToList();
        AssertSubmittedEndpoints(submitted);

        var submittedIds = submitted.Where(x => x.Id != 0).Select(x => x.Id).ToHashSet();

        // 从集合移除后，已加载跟踪的接入点及其映射在保存时一并软删
        foreach (var endpoint in Endpoints.Where(x => !submittedIds.Contains(x.Id)).ToList())
        {
            Endpoints.Remove(endpoint);
        }

        foreach (var input in submitted)
        {
            var existing = input.Id == 0 ? null : Endpoints.FirstOrDefault(x => x.Id == input.Id);
            if (existing == null)
            {
                var endpoint = ModelProviderEndpoint.Create(input.Protocol, input.Name, input.BaseUrl, input.ApiKey, input.Setting);
                endpoint.SyncMappings(input.Mappings);
                Endpoints.Add(endpoint);
            }
            else
            {
                existing.Update(input.Name, input.BaseUrl, input.ApiKey, input.Setting);
                existing.SyncMappings(input.Mappings);
            }
        }
    }

    /// <summary>
    /// 聚合内部不变量：同一个接入点只提交一次、接入点名称互不重复；
    /// 带 Id 的必须是名下已有的接入点（调用前聚合已连同接入点加载），且协议创建后不可变。
    /// </summary>
    private void AssertSubmittedEndpoints(List<EndpointInput> submitted)
    {
        if (submitted.Where(x => x.Id != 0).GroupBy(x => x.Id).Any(g => g.Count() > 1))
        {
            throw new UserFriendlyException("同一个接入点不能提交两次。");
        }

        var duplicateName = submitted.GroupBy(x => x.Name).FirstOrDefault(g => g.Count() > 1)?.Key;
        if (duplicateName != null)
        {
            throw new UserFriendlyException($"接入点名称「{duplicateName}」重复。");
        }

        foreach (var input in submitted.Where(x => x.Id != 0))
        {
            var existing = Endpoints.FirstOrDefault(x => x.Id == input.Id);
            if (existing == null)
            {
                throw new UserFriendlyException($"接入点「{input.Name}」不存在。");
            }

            if (existing.Protocol != input.Protocol)
            {
                throw new UserFriendlyException($"接入点「{input.Name}」的协议创建后不可修改，如需更换请删除后重建。");
            }
        }
    }
}
