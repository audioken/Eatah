using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Eatah.Infrastructure.Persistence.Configurations;

/// <summary>
/// Compatibility helpers for Npgsql EF Core 10, which removed the
/// <c>UseXminAsConcurrencyToken</c> extension. We map the PostgreSQL
/// <c>xmin</c> system column as a shadow <see cref="uint"/> row version
/// instead, which the Npgsql provider understands natively.
/// </summary>
internal static class NpgsqlBuilderExtensions
{
    public static EntityTypeBuilder<T> UseXminAsConcurrencyToken<T>(this EntityTypeBuilder<T> builder)
        where T : class
    {
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        return builder;
    }
}
