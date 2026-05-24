using Eatah.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eatah.Infrastructure.Persistence.Configurations;

public class ChatThreadConfiguration : IEntityTypeConfiguration<ChatThread>
{
    public void Configure(EntityTypeBuilder<ChatThread> b)
    {
        b.ToTable("chat_threads");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.HasMany(x => x.Messages).WithOne(m => m.Thread!).HasForeignKey(m => m.ThreadId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Participants).WithOne(p => p.Thread!).HasForeignKey(p => p.ThreadId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.WorkspaceId);
    }
}

public class ChatThreadParticipantConfiguration : IEntityTypeConfiguration<ChatThreadParticipant>
{
    public void Configure(EntityTypeBuilder<ChatThreadParticipant> b)
    {
        b.ToTable("chat_thread_participants");
        b.HasKey(x => new { x.ThreadId, x.UserId });
        b.Property(x => x.ThreadId).HasColumnName("thread_id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.HasIndex(x => x.UserId);
    }
}

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> b)
    {
        b.ToTable("chat_messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ThreadId).HasColumnName("thread_id");
        b.Property(x => x.AuthorUserId).HasColumnName("author_user_id");
        b.Property(x => x.Text).HasColumnName("text").IsRequired().HasMaxLength(2000);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.Property(x => x.EditedAt).HasColumnName("edited_at");
        b.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        b.HasMany(x => x.Reactions).WithOne(r => r.Message!).HasForeignKey(r => r.MessageId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.ThreadId, x.CreatedAt });
    }
}

public class ChatReactionConfiguration : IEntityTypeConfiguration<ChatReaction>
{
    public void Configure(EntityTypeBuilder<ChatReaction> b)
    {
        b.ToTable("chat_reactions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.MessageId).HasColumnName("message_id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Emoji).HasColumnName("emoji").IsRequired().HasMaxLength(8);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.HasIndex(x => new { x.MessageId, x.UserId, x.Emoji }).IsUnique();
    }
}

public class ChatThreadReadStatusConfiguration : IEntityTypeConfiguration<ChatThreadReadStatus>
{
    public void Configure(EntityTypeBuilder<ChatThreadReadStatus> b)
    {
        b.ToTable("chat_thread_read_statuses");
        b.HasKey(x => new { x.UserId, x.ThreadId });
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.ThreadId).HasColumnName("thread_id");
        b.Property(x => x.LastReadAt).HasColumnName("last_read_at");
        b.HasIndex(x => x.ThreadId);
    }
}
