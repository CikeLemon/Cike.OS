using Cike.OS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.AI;

namespace Cike.OS.EntityFrameworkCore.EntityConfigurations;

public class ConversationEntityConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Title)
            .HasMaxLength(128);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.AgentId);
    }
}

public class ConversationMemberEntityConfiguration : IEntityTypeConfiguration<ConversationMember>
{
    public void Configure(EntityTypeBuilder<ConversationMember> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.MemberId);
        builder.HasIndex(x => new { x.TenantId, x.ConversationId, x.MemberId });
    }
}

public class ConversationFileEntityConfiguration : IEntityTypeConfiguration<ConversationFile>
{
    public void Configure(EntityTypeBuilder<ConversationFile> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(256);

        builder.Property(x => x.MimeType)
            .HasMaxLength(128);

        builder.Property(x => x.StoragePath)
            .HasMaxLength(512);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.Sender);
    }
}

public class ConversationMessageEntityConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SenderRole)
            .HasConversion(
                role => role.Value,
                value => new ChatRole(value))
            .HasMaxLength(64);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.SenderId);
    }
}

public class ConversationMessageEventEntityConfiguration : IEntityTypeConfiguration<ConversationMessageEvent>
{
    public void Configure(EntityTypeBuilder<ConversationMessageEvent> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ActionId)
            .HasMaxLength(64);

        builder.Property(x => x.ActionName)
            .HasMaxLength(128);

        builder.Property(x => x.Type)
            .HasMaxLength(64);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.ConversationMessageId);
    }
}
