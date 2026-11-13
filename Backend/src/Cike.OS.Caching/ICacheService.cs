namespace Cike.Workflow.Caching;

public interface ICacheService<TModel> where TModel : EntityDto<long>, IMultiTenant
{
    Task SetAsync(TModel model, CancellationToken cancellationToken = default);

    Task SetListAsync(IEnumerable<TModel> models, CancellationToken cancellationToken = default);

    Task RemoveAsync(long id, CancellationToken cancellationToken = default);

    Task RemoveRangeAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);

    Task<IEnumerable<TModel>> GetListAsync(Expression<Func<TModel, bool>>? filter = null, CancellationToken cancellationToken = default);

    Task<TModel?> GetAsync(long id, CancellationToken cancellationToken = default);
}
