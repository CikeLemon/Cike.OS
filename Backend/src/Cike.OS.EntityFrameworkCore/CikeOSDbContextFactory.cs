using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cike.OS.EntityFrameworkCore;

public class CikeOSDbContextFactory : IDesignTimeDbContextFactory<CikeOSDbContext>
{
    public CikeOSDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CikeOSDbContext>();
        optionsBuilder.UseMySql(
            "Server=localhost;Database=cike_OS;User=root;Password=;",
            new MySqlServerVersion(new Version(8, 0, 36)));

        return new CikeOSDbContext(optionsBuilder.Options, null!);
    }
}
