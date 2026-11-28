using Cike.OS.Domain.Entities;
using Cike.OS.Domain.Shared.Json;
using Cike.OS.Domain.Shared.ValueObjects;
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

        // 指令是 Agent 的自我介绍与提示词，不限长度（落 longtext）

        builder.Property(x => x.Options)
            .HasConversion(
                options => JsonHelper.Serialize(options),
                json => JsonHelper.Deserialize<AgentOptions>(json) ?? new AgentOptions())
            .HasColumnType("json");

        // Agent 工具是聚合内的子实体：关系配在聚合这一侧，级联删除走客户端级联（连同集合加载后子项跟着软删）
        builder.HasMany(x => x.Tools)
            .WithOne()
            .HasForeignKey(x => x.AgentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TenantId);
    }
}

public class AgentToolEntityConfiguration : IEntityTypeConfiguration<AgentTool>
{
    public void Configure(EntityTypeBuilder<AgentTool> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.AgentId);
        builder.HasIndex(x => x.TargetId);
    }
}
