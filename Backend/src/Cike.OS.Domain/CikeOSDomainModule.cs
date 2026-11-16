using Cike.Core.Modularity;
using Cike.Data.Domain;
using Cike.OS.Domain.Shared;

namespace Cike.OS.Domain;

[DependsOn([
    typeof(CikeDomainModule),

    typeof(CikeOSDomainSharedModule)
    ])]
public class CikeOSDomainModule : CikeModule
{
}
