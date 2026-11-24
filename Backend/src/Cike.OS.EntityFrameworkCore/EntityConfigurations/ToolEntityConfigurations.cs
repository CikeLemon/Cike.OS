using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Shared.Json;
using Cike.OS.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
                parameters => JsonHelper.Serialize(parameters),
                json => JsonHelper.Deserialize<List<ToolParameterDefinition>>(json) ?? new List<ToolParameterDefinition>())
            .HasColumnType("json");

        // 多态值对象：JsonHelper.Options 允许判别键 type 不在首位（MySQL json 列会重排成员）
        builder.Property(x => x.Setting)
            .HasConversion(
                setting => JsonHelper.Serialize(setting),
                json => JsonColumn.Required<ToolSetting>(json))
            .HasColumnType("json");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ToolPackageId);
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
                values => JsonHelper.Serialize(values),
                json => JsonHelper.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>())
            .HasColumnType("json");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ToolId);
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.ToolId });
    }
}
