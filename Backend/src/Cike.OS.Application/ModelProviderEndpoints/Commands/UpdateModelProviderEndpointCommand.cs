using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;

namespace Cike.OS.Application.ModelProviderEndpoints.Commands;

/// <summary>接入点 Id 在路由上，DTO 只承载可变字段（归属与协议不可变，不在其中）。</summary>
public record UpdateModelProviderEndpointCommand(long Id, UpdateModelProviderEndpointDto Dto) : Command;
