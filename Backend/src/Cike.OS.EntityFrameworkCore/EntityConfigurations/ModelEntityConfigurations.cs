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

        // 提供商映射是接入点下的子实体，随聚合整体加载 / 软删
        builder.HasIndex(x => x.TenantId);

        builder.HasIndex(x => x.ModelProviderEndpointId);

        builder.HasIndex(x => x.ModelId);
    }
}

public class ModelProviderEndpointEntityConfiguration : IEntityTypeConfiguration<ModelProviderEndpoint>
{
    public void Configure(EntityTypeBuilder<ModelProviderEndpoint> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(128);

        builder.Property(x => x.Setting)
            .HasConversion(
                setting => JsonHelper.Serialize(setting),
                json => JsonColumn.Required<ModelProviderEndpointSetting>(json))
            .HasColumnType("json");

        // 接入点与模型提供方是两个聚合，按 Id 引用：关系配在接入点一侧，提供方上没有导航集合
        builder.HasOne<ModelProvider>()
            .WithMany()
            .HasForeignKey(x => x.ModelProviderId)
            .OnDelete(DeleteBehavior.Cascade);

        // 提供商映射是接入点下的子实体，随聚合整体加载 / 软删
        builder.HasMany(x => x.Mappings)
            .WithOne()
            .HasForeignKey(x => x.ModelProviderEndpointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ModelProviderId);
    }
}

public class ModelProviderEntityConfiguration : IEntityTypeConfiguration<ModelProvider>
{
    public void Configure(EntityTypeBuilder<ModelProvider> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(128);

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
