using Cike.Cqrs.Commands;
using Cike.OS.Application.Contracts.ModelProviders.Dtos;

namespace Cike.OS.Application.ModelProviders.Commands;

/// <summary>Id 在路由，DTO 承载连接字段与期望的完整模型列表。</summary>
public record UpdateModelProviderCommand(long Id, UpdateModelProviderDto Dto) : Command;
