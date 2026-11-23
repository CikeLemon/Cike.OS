using Cike.OS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Cike.OS.EntityFrameworkCore.EntityConfigurations;

public class UserToolConfigurationEntityConfiguration : IEntityTypeConfiguration<UserToolConfiguration>
{
    public void Configure(EntityTypeBuilder<UserToolConfiguration> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasMaxLength(64);

        builder.Property(x => x.Values)
            .HasConversion(
                values => JsonSerializer.Serialize(values, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
            .HasColumnType("json");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ToolId);
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.ToolId });
    }
}
