using Cike.Data.Domain.Repositories;
using Cike.OS.Domain.Entities;

namespace Cike.OS.Domain.Repositories;

/// <summary>
/// 模型提供方仓储：在通用仓储之上提供「连同两层子集合（接入点 → 映射）加载单个聚合」的方法。
/// 通用仓储的 WithDetails 只能预加载一级导航，要改这个聚合必须走这里把它加载全。
/// 列表不用它：列表只做投影查询，不加载聚合。
/// </summary>
public interface IModelProviderRepository : IRepository<ModelProvider, long>
{
    /// <summary>
    /// 连同接入点与映射加载一个提供方。默认跟踪，用于修改聚合、删除聚合；
    /// 改聚合前必须用它加载，子实体的增删改与随聚合软删才成立。详情页读取传 asNoTracking: true。
    /// </summary>
    Task<ModelProvider?> FindWithEndpointsAsync(long id, bool asNoTracking = false, CancellationToken cancellationToken = default);
}
