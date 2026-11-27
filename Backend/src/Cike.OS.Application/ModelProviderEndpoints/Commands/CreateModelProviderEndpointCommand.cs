using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;

namespace Cike.OS.Application.ModelProviderEndpoints.Commands;

/// <summary>新建一个接入点（含它的全部映射）；Id 为新接入点的 Id。</summary>
public record CreateModelProviderEndpointCommand(CreateModelProviderEndpointDto Dto) : Command
{
    public long Id { get; set; }
}
