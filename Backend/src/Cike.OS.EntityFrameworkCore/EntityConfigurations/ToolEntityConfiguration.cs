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
            .HasMaxLength(1024);

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

        builder.HasIndex(x => x.ToolPackageId);
    }

    private static ToolSetting DeserializeToolSetting(string json)
    {
        return JsonSerializer.Deserialize<ToolSetting>(json, (JsonSerializerOptions?)null)
            ?? throw new InvalidOperationException("Tool setting JSON is invalid.");
    }
}
