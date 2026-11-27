using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;

namespace Cike.OS.Application.ModelProviders.Commands;

/// <summary>给提供方添加一个接入点（含它的全部映射）；Id 为新接入点的 Id。</summary>
public record CreateModelProviderEndpointCommand(long ProviderId, CreateModelProviderEndpointDto Dto) : Command
{
    public long Id { get; set; }
}
