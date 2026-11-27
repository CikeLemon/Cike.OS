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

        builder.Property(x => x.Description)
            .HasMaxLength(2048);

        builder.Property(x => x.TagIds)
            .HasConversion(
                tagIds => JsonHelper.Serialize(tagIds),
                json => JsonHelper.Deserialize<List<long>>(json) ?? new List<long>())
            .HasColumnType("json");

        // 模型是模型提供方聚合内的子实体，随聚合整体加载 / 软删
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

        builder.Property(x => x.Setting)
            .HasConversion(
                setting => JsonHelper.Serialize(setting),
                json => JsonColumn.Required<ModelProviderSetting>(json))
            .HasColumnType("json");

        // 模型是提供方聚合内的子实体：关系配在聚合这一侧，级联删除走客户端级联（连同集合加载后子项跟着软删）
        builder.HasMany(x => x.Models)
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
