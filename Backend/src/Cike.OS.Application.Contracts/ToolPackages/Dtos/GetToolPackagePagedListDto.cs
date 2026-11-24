using Cike.Contracts.EntityDtos;
using Cike.OS.Domain.Shared.Enums;

namespace Cike.OS.Application.Contracts.ToolPackages.Dtos;

public class GetToolPackagePagedListDto : PagedAndSortedResultRequest
{
    /// <summary>按名称与编码模糊匹配。</summary>
    public string? Keyword { get; set; }

    public long? ToolPackageCategoryId { get; set; }

    public ToolPackageStatus? Status { get; set; }
}
