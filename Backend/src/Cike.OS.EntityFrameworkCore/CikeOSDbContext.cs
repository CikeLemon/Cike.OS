using Cike.Data.EFCore;
using Cike.OS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cike.OS.EntityFrameworkCore;

public class CikeOSDbContext : CikeDbContext<CikeOSDbContext>
{
    public CikeOSDbContext(DbContextOptions<CikeOSDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }

    public virtual DbSet<Tool> Tools { get; set; }

    public virtual DbSet<ToolPackage> ToolPackages { get; set; }

    public virtual DbSet<ToolPackageCategory> ToolPackageCategories { get; set; }

    public virtual DbSet<UserToolConfiguration> UserToolConfigurations { get; set; }

    public virtual DbSet<AgentEntity> Agents { get; set; }

    public virtual DbSet<AgentToolPackage> AgentToolPackages { get; set; }

    public virtual DbSet<AgentTool> AgentTools { get; set; }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<ConversationMember> ConversationMembers { get; set; }

    public virtual DbSet<ConversationFile> ConversationFiles { get; set; }

    public virtual DbSet<ConversationMessage> ConversationMessages { get; set; }

    public virtual DbSet<ConversationMessageEvent> ConversationMessageEvents { get; set; }

    public virtual DbSet<ModelEntity> Models { get; set; }

    public virtual DbSet<ModelProviderEndpoint> ModelProviderEndpoints { get; set; }

    public virtual DbSet<ModelProviderMapping> ModelProviderMappings { get; set; }

    public virtual DbSet<ModelProvider> ModelProviders { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CikeOSDbContext).Assembly);
    }
}
