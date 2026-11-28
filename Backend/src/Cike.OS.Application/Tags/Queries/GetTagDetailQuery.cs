using Cike.Cqrs.Queries;
using Cike.OS.Application.Contracts.Tags.Dtos;

namespace Cike.OS.Application.Tags.Queries;

/// <summary>按 Id 取标签详情（编辑表单回填）。</summary>
public record GetTagDetailQuery(long Id) : Query<TagDetailDto?>;
