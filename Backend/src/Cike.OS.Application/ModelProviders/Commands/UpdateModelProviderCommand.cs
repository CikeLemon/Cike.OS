using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;

namespace Cike.OS.Application.ModelProviders.Commands;

/// <summary>Id 在路由，DTO 只承载可变字段（提供方本身只有名称）。</summary>
public record UpdateModelProviderCommand(long Id, UpdateModelProviderDto Dto) : Command;
