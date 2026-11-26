using Cike.Contracts.EntityDtos;

namespace Cike.OS.Application.Contracts.Models.Dtos;

public class GetModelPagedListDto : PagedAndSortedResultRequest
{
    /// <summary>按模型名称与显示名称模糊匹配。</summary>
    public string? Keyword { get; set; }
}
