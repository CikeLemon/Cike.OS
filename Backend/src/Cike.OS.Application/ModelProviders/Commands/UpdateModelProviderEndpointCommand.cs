using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;

namespace Cike.OS.Application.ModelProviders.Commands;

/// <summary>提供方 Id 与接入点 Id 都在路由上，DTO 只承载可变字段（协议不可变，不在其中）。</summary>
public record UpdateModelProviderEndpointCommand(long ProviderId, long EndpointId, UpdateModelProviderEndpointDto Dto) : Command;
