using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Shared.Json;
using Cike.OS.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cike.OS.EntityFrameworkCore.EntityConfigurations;

public class AgentEntityConfiguration : IEntityTypeConfiguration<AgentEntity>
{
    public void Configure(EntityTypeBuilder<AgentEntity> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(128);

        builder.Property(x => x.Description)
            .HasMaxLength(512);

        builder.Property(x => x.Options)
            .HasConversion(
                options => JsonHelper.Serialize(options),
                json => JsonHelper.Deserialize<AgentOptions>(json) ?? new AgentOptions())
            .HasColumnType("json");

        builder.HasIndex(x => x.TenantId);
    }
}

public class AgentToolPackageEntityConfiguration : IEntityTypeConfiguration<AgentToolPackage>
{
    public void Configure(EntityTypeBuilder<AgentToolPackage> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasMany(x => x.AgentTools)
            .WithOne()
            .HasForeignKey(x => x.AgentToolPackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.AgentId);
        builder.HasIndex(x => x.ToolPackageId);
    }
}

public class AgentToolEntityConfiguration : IEntityTypeConfiguration<AgentTool>
{
    public void Configure(EntityTypeBuilder<AgentTool> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.AgentToolPackageId);
        builder.HasIndex(x => x.ToolId);
    }
}
