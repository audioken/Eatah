using Eatah.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eatah.Infrastructure.Persistence.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> b)
    {
        b.ToTable("push_subscriptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Endpoint).HasColumnName("endpoint").IsRequired().HasMaxLength(1000);
        b.Property(x => x.P256dh).HasColumnName("p256dh").IsRequired().HasMaxLength(300);
        b.Property(x => x.Auth).HasColumnName("auth").IsRequired().HasMaxLength(100);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Endpoint).IsUnique();
    }
}
