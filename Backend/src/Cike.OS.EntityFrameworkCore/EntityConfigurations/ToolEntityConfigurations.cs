using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Shared.ValueObjects;
using Cike.OS.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Cike.OS.EntityFrameworkCore.EntityConfigurations;

public class ToolEntityConfiguration : IEntityTypeConfiguration<Tool>
{
    public void Configure(EntityTypeBuilder<Tool> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasMaxLength(64);

        builder.Property(x => x.Name)
            .HasMaxLength(128);

        builder.Property(x => x.Description)
            .HasMaxLength(512);

        builder.Property(x => x.Parameters)
            .HasConversion(
                parameters => JsonSerializer.Serialize(parameters, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<ToolParameterDefinition>>(json, (JsonSerializerOptions?)null) ?? new List<ToolParameterDefinition>())
            .HasColumnType("json");

        builder.Property(x => x.Setting)
            .HasConversion(
                setting => JsonSerializer.Serialize(setting, (JsonSerializerOptions?)null),
                json => DeserializeToolSetting(json))
            .HasColumnType("json");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ToolPackageId);
    }

    private static ToolSetting DeserializeToolSetting(string json)
    {
        return JsonSerializer.Deserialize<ToolSetting>(json, (JsonSerializerOptions?)null)
            ?? throw new InvalidOperationException("Tool setting JSON is invalid.");
    }
}

public class ToolPackageEntityConfiguration : IEntityTypeConfiguration<ToolPackage>
{
    public void Configure(EntityTypeBuilder<ToolPackage> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasMaxLength(64);

        builder.Property(x => x.Name)
            .HasMaxLength(128);

        builder.Property(x => x.Description)
            .HasMaxLength(512);

        builder.Property(x => x.Icon)
            .HasMaxLength(256);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ToolPackageCategoryId);
    }
}

public class ToolPackageCategoryEntityConfiguration : IEntityTypeConfiguration<ToolPackageCategory>
{
    public void Configure(EntityTypeBuilder<ToolPackageCategory> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(128);

        builder.HasIndex(x => x.TenantId);
    }
}

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
