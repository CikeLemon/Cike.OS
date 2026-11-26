using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Shared.Json;
using Cike.OS.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cike.OS.EntityFrameworkCore.EntityConfigurations;

public class ModelEntityConfiguration : IEntityTypeConfiguration<ModelEntity>
{
    public void Configure(EntityTypeBuilder<ModelEntity> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(128);

        builder.Property(x => x.DisplayName)
            .HasMaxLength(128);

        builder.Property(x => x.Description)
            .HasMaxLength(2048);

        builder.Property(x => x.TagIds)
            .HasConversion(
                tagIds => JsonHelper.Serialize(tagIds),
                json => JsonHelper.Deserialize<List<long>>(json) ?? new List<long>())
            .HasColumnType("json");
    }
}

public class ModelProviderMappingEntityConfiguration : IEntityTypeConfiguration<ModelProviderMapping>
{
    public void Configure(EntityTypeBuilder<ModelProviderMapping> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ProviderModelName)
            .HasMaxLength(128);

        // 提供商映射是 ModelProvider 聚合内的子实体，随聚合整体加载 / 软删
        builder.HasIndex(x => x.TenantId);

        builder.HasIndex(x => x.ModelId);
    }
}

public class ModelProviderEntityConfiguration : IEntityTypeConfiguration<ModelProvider>
{
    public void Configure(EntityTypeBuilder<ModelProvider> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(128);

        builder.Property(x => x.Setting)
            .HasConversion(
                setting => JsonHelper.Serialize(setting),
                json => JsonColumn.Required<ModelProviderSetting>(json))
            .HasColumnType("json");

        builder.HasMany(x => x.Mappings)
            .WithOne()
            .HasForeignKey(x => x.ModelProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TenantId);
    }
}

public class TagEntityConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Key)
            .HasMaxLength(64);

        builder.Property(x => x.Name)
            .HasMaxLength(128);
    }
}
