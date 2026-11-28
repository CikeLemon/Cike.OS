using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.Agents.Dtos;

namespace Cike.OS.Application.Agents.Queries;

public record GetAgentDetailQuery(long Id) : Query<AgentDetailDto?>;
