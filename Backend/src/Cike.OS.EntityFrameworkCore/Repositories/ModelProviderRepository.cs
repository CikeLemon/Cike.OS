using Cike.Core.DependencyInjection;
using Cike.Data.EFCore.Repositories;
using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Cike.OS.EntityFrameworkCore.Repositories;

/// <summary>
/// 模型提供方仓储实现：用两级 Include 把「提供方 → 接入点 → 映射」一次查全，供详情读取与修改聚合使用。
/// 软删 / 多租户过滤器是 EF 全局查询过滤器，直接查 DbSet 同样生效。
/// 标注 IScopedDependency 走约定注册，先于默认仓储注册，自动接管 IRepository&lt;ModelProvider, long&gt; 的解析。
/// 列表不走这里：列表在 Application 里做投影查询，不加载聚合。
/// </summary>
public class ModelProviderRepository(CikeOSDbContext dbContext)
    : EfCoreRepository<CikeOSDbContext, ModelProvider, long>(dbContext), IModelProviderRepository, IScopedDependency
{
    public async Task<ModelProvider?> FindWithEndpointsAsync(long id, bool asNoTracking = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ModelProviders
            .Include(x => x.Endpoints)
            .ThenInclude(x => x.Mappings);

        return asNoTracking
            ? await query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            : await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
