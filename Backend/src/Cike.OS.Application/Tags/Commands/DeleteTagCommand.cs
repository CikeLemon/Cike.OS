using Cike.Cqrs.Commands;

namespace Cike.OS.Application.Tags.Commands;

/// <summary>删除不做引用守护：被模型引用也直接删，模型侧读取时把已不存在的标签当作不存在。</summary>
public record DeleteTagCommand(long Id) : Command;
