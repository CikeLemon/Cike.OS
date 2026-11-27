using Cike.Data.Domain.Repositories;
using Cike.EventBus.Local;
using Cike.OS.Application.Tags.Commands;
using Cike.OS.Domain.Entities;

namespace Cike.OS.Application.Tags;

public class TagCommandHandler(IRepository<Tag, long> tagRepository)
{
    [LocalEventHandler]
    public async Task CreateAsync(CreateTagCommand command, CancellationToken cancellationToken = default)
    {
        var dto = command.Dto;

        // 键全局唯一（标签是平台级目录，不分租户）；软删过滤生效，已删的同键不算重复，并发窗口按 ADR 0001 接受
        if (await tagRepository.GetCountAsync(x => x.Key == dto.Key, cancellationToken) > 0)
        {
            throw new UserFriendlyException($"键「{dto.Key}」已存在。");
        }

        var tag = Tag.Create(dto.Key, dto.Name);
        await tagRepository.InsertAsync(tag, cancellationToken: cancellationToken);
        command.Id = tag.Id;
    }

    [LocalEventHandler]
    public async Task UpdateAsync(UpdateTagCommand command, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.FindAsync(command.Id, cancellationToken)
            ?? throw new UserFriendlyException("标签不存在。");

        tag.Rename(command.Dto.Name);
        await tagRepository.UpdateAsync(tag, cancellationToken: cancellationToken);
    }

    [LocalEventHandler]
    public async Task DeleteAsync(DeleteTagCommand command, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.FindAsync(command.Id, cancellationToken)
            ?? throw new UserFriendlyException("标签不存在。");

        // 不检查模型引用、不改模型数据：残留的引用在模型分页列表读取时被当作不存在
        await tagRepository.DeleteAsync(tag, cancellationToken: cancellationToken);
    }
}
