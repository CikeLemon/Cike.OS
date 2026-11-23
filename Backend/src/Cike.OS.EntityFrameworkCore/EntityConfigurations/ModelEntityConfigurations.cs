using Cike.OS.Domain.Entities;
using Cike.OS.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

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

        builder.Property(x => x.TagIds)
            .HasConversion(
                tagIds => JsonSerializer.Serialize(tagIds, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<long>>(json, (JsonSerializerOptions?)null) ?? new List<long>())
            .HasColumnType("json");

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
                setting => JsonSerializer.Serialize(setting, (JsonSerializerOptions?)null),
                json => DeserializeModelProviderSetting(json))
            .HasColumnType("json");

        builder.HasIndex(x => x.TenantId);
    }

    private static ModelProviderSetting DeserializeModelProviderSetting(string json)
    {
        return JsonSerializer.Deserialize<ModelProviderSetting>(json, (JsonSerializerOptions?)null)
            ?? throw new InvalidOperationException("Model provider setting JSON is invalid.");
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

        builder.HasIndex(x => x.TenantId);
    }
}
