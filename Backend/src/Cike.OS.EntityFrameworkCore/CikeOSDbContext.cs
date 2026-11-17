using Cike.Data.EFCore;
using Cike.OS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cike.OS.EntityFrameworkCore;

public class CikeOSDbContext : CikeDbContext<CikeOSDbContext>
{
    public CikeOSDbContext(DbContextOptions<CikeOSDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }

    public DbSet<Tool> Tools => Set<Tool>();

    public DbSet<UserToolConfiguration> UserToolConfigurations => Set<UserToolConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CikeOSDbContext).Assembly);
    }
}
