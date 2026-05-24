using Eatah.Domain.Entities;
using Eatah.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Infrastructure.Persistence;

public class EatahDbContext : IdentityDbContext<EatahUser, IdentityRole<Guid>, Guid>
{
    public EatahDbContext(DbContextOptions<EatahDbContext> options) : base(options)
    {
    }

    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<WeeklyPlan> WeeklyPlans => Set<WeeklyPlan>();
    public DbSet<DayPlan> DayPlans => Set<DayPlan>();
    public DbSet<DietProfile> DietProfiles => Set<DietProfile>();
    public DbSet<DietRule> DietRules => Set<DietRule>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<IngredientMaster> IngredientMaster => Set<IngredientMaster>();
    public DbSet<PantryItem> PantryItems => Set<PantryItem>();
    public DbSet<PantryItemMealCoverage> PantryItemMealCoverages => Set<PantryItemMealCoverage>();
    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();
    public DbSet<ChatThread> ChatThreads => Set<ChatThread>();
    public DbSet<ChatThreadParticipant> ChatThreadParticipants => Set<ChatThreadParticipant>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatReaction> ChatReactions => Set<ChatReaction>();
    public DbSet<ChatThreadReadStatus> ChatThreadReadStatuses => Set<ChatThreadReadStatus>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EatahDbContext).Assembly);
        MapIdentityTablesToSnakeCase(modelBuilder);
    }

    /// <summary>
    /// Renames the default ASP.NET Identity tables (AspNet*) and their columns to snake_case,
    /// to stay consistent with the rest of the schema.
    /// </summary>
    private static void MapIdentityTablesToSnakeCase(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EatahUser>(b =>
        {
            b.ToTable("users");
            b.Property(u => u.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(50);
            b.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            b.HasIndex(u => u.DisplayName)
                .IsUnique()
                .HasDatabaseName("ix_users_display_name");
        });

        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        var identityTables = new HashSet<string>
        {
            "users", "roles", "user_roles", "user_claims", "user_logins", "role_claims", "user_tokens"
        };

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is null || !identityTables.Contains(tableName))
            {
                continue;
            }

            foreach (var property in entityType.GetProperties())
            {
                var current = property.GetColumnName();
                if (string.IsNullOrEmpty(current) || current.Contains('_'))
                {
                    continue; // already mapped explicitly (display_name, created_at)
                }
                property.SetColumnName(ToSnakeCase(current));
            }

            foreach (var key in entityType.GetKeys())
            {
                var name = key.GetName();
                if (!string.IsNullOrEmpty(name) && !name.Contains('_'))
                {
                    key.SetName(ToSnakeCase(name));
                }
            }

            foreach (var index in entityType.GetIndexes())
            {
                var name = index.GetDatabaseName();
                if (!string.IsNullOrEmpty(name) && !name.Contains('_'))
                {
                    index.SetDatabaseName(ToSnakeCase(name));
                }
            }

            foreach (var fk in entityType.GetForeignKeys())
            {
                var name = fk.GetConstraintName();
                if (!string.IsNullOrEmpty(name) && !name.Contains('_'))
                {
                    fk.SetConstraintName(ToSnakeCase(name));
                }
            }
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var sb = new System.Text.StringBuilder(input.Length + 8);
        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c) && i > 0 &&
                (char.IsLower(input[i - 1]) || (i + 1 < input.Length && char.IsLower(input[i + 1]))))
            {
                sb.Append('_');
            }
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
