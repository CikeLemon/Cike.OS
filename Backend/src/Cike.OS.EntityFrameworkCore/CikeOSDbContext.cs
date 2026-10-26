using Cike.Data.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Cike.OS.EntityFrameworkCore
{
    public class CikeOSDbContext : CikeDbContext<CikeOSDbContext>
    {
        public CikeOSDbContext(DbContextOptions<CikeOSDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
        {
        }
    }
}
