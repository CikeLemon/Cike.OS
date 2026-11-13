using Cike.Auth.MultiTenant;

namespace Cike.Workflow.Caching.Internals;

internal class BaseCacheService<TModel>(IMultilevelCacheClient multilevelCacheClient, ICurrentTenant currentTenant) : ICacheService<TModel> where TModel : EntityDto<long>, IMultiTenant
{
    private string GetIdListKey => $"{typeof(TModel).Name}:{currentTenant.Id.ToString()}";

    public async Task<TModel?> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        return await multilevelCacheClient.GetAsync<TModel>(id.ToString());
    }

    public async Task<IEnumerable<TModel>> GetListAsync(Expression<Func<TModel, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= e => true;
        var idList = await multilevelCacheClient.GetAsync<List<long>>(GetIdListKey);
        var result = await multilevelCacheClient.GetListAsync<TModel>(idList!.Select(e => e.ToString()).ToArray());
        return result.Where(e => e != null).Where(filter.Compile()!).Select(e => e!).ToList();
    }

    public async Task RemoveAsync(long id, CancellationToken cancellationToken = default)
    {
        await RemoveRangeAsync([id], cancellationToken);
    }

    public async Task RemoveRangeAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
    {
        await multilevelCacheClient.RemoveAsync<TModel>(ids.Select(e => e.ToString()).ToArray());
        await RemoveIdListAsync(ids, cancellationToken);
    }

    public async Task SetAsync(TModel model, CancellationToken cancellationToken = default)
    {
        await SetListAsync([model], cancellationToken);
    }

    public async Task SetListAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default)
    {
        await multilevelCacheClient.SetListAsync(models.ToDictionary(e => e.Id.ToString(), e => e ?? null));
        await SetIdListAsync(models.Select(e => e.Id), cancellationToken);
    }

    private async Task SetIdListAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
    {
        var list = (await multilevelCacheClient.GetAsync<List<long>>(GetIdListKey)) ?? new List<long>();
        foreach (var id in ids)
        {
            if (!list.Contains(id))
                list.Add(id);
        }
        await multilevelCacheClient.SetAsync(GetIdListKey, list);
    }

    private async Task RemoveIdListAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
    {
        var list = await multilevelCacheClient.GetAsync<List<long>>(GetIdListKey);
        foreach (var id in ids)
        {
            if (list.Contains(id))
                list.Remove(id);
        }
        await multilevelCacheClient.SetAsync(GetIdListKey, list);
    }
}
