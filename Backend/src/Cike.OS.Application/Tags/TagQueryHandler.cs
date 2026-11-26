using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.Tags.Dtos;
using Cike.OS.Application.Tags.Queries;
using Cike.OS.Domain.Entities;
using Mapster;

namespace Cike.OS.Application.Tags;

public class TagQueryHandler(IReadOnlyRepository<Tag, long> tagRepository)
{
    [LocalEventHandler]
    public async Task GetListAsync(GetTagListQuery query, CancellationToken cancellationToken = default)
    {
        using var _ = tagRepository.BeginAsNoTracking();

        var tags = await tagRepository.ToListAsync(
            tagRepository.GetQueryable().OrderBy(x => x.Key),
            cancellationToken);
        query.Result = tags.Adapt<List<TagDto>>();
    }
}
