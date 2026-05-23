using Eatah.Domain.Entities;
using Eatah.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Eatah.Infrastructure.Persistence;

public static class DataSeeder
{
    public const string DevUserEmail = "dev@eatah.local";
    public const string DevUserDisplayName = "Dev";
    public const string DevUserPassword = "Dev123!@#";

    public static async Task SeedAsync(EatahDbContext context, CancellationToken cancellationToken = default)
    {
        await SeedDietProfilesAsync(context, cancellationToken);
        await SeedSystemIngredientsAsync(context, cancellationToken);
    }

    /// <summary>
    /// Seeds a confirmed development user via Identity's <see cref="UserManager{TUser}"/>.
    /// Idempotent: skipped if a user with <see cref="DevUserEmail"/> already exists.
    /// </summary>
    public static async Task SeedDevUserAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<EatahUser>>();
        var context = serviceProvider.GetRequiredService<EatahDbContext>();
        var existing = await userManager.FindByEmailAsync(DevUserEmail);
        if (existing is not null)
        {
            await EnsureDefaultHouseholdAsync(context, existing.Id, cancellationToken);
            return;
        }

        var user = new EatahUser
        {
            Id = Guid.NewGuid(),
            UserName = DevUserDisplayName,
            Email = DevUserEmail,
            EmailConfirmed = true,
            DisplayName = DevUserDisplayName
        };

        var result = await userManager.CreateAsync(user, DevUserPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Failed to seed dev user: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }
        await EnsureDefaultHouseholdAsync(context, user.Id, cancellationToken);
    }

    /// <summary>
    /// Idempotently ensures the user belongs to a household. Safe to call repeatedly.
    /// </summary>
    public static async Task EnsureDefaultHouseholdAsync(EatahDbContext context, Guid userId, CancellationToken cancellationToken = default)
    {
        var hasHousehold = await context.WorkspaceMembers
            .AnyAsync(m => m.UserId == userId, cancellationToken);
        if (hasHousehold) return;

        var ws = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "Mitt hushåll",
            Members =
            [
                new WorkspaceMember { UserId = userId, Role = MemberRole.Owner }
            ]
        };
        await context.Workspaces.AddAsync(ws, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedDietProfilesAsync(EatahDbContext context, CancellationToken cancellationToken)
    {
        if (await context.DietProfiles.AnyAsync(cancellationToken)) return;

        var defaultProfile = new DietProfile
        {
            Id = Guid.NewGuid(),
            Name = "Livsmedelsverket",
            Rules =
            [
                new DietRule { Id = Guid.NewGuid(), Category = MealCategory.Fish, MinPerWeek = 2, MaxPerWeek = 3, Description = "Ät fisk 2–3 gånger per vecka." },
                new DietRule { Id = Guid.NewGuid(), Category = MealCategory.Meat, MinPerWeek = 0, MaxPerWeek = 3, Description = "Begränsa rött och processat kött till högst 3 gånger per vecka." },
                new DietRule { Id = Guid.NewGuid(), Category = MealCategory.Poultry, MinPerWeek = 1, MaxPerWeek = 3, Description = "Ät fågel med måtta, förslagsvis 1–3 gånger per vecka." },
                new DietRule { Id = Guid.NewGuid(), Category = MealCategory.Vegetarian, MinPerWeek = 2, MaxPerWeek = 7, Description = "Ät vegetariskt minst 2 gånger per vecka." }
            ]
        };

        await context.DietProfiles.AddAsync(defaultProfile, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Seeds a small system catalog of common Swedish ingredients. Each is workspace-less
    /// (WorkspaceId == null) so visible to every workspace. Idempotent.
    /// </summary>
    private static async Task SeedSystemIngredientsAsync(EatahDbContext context, CancellationToken cancellationToken)
    {
        if (await context.IngredientMaster.AnyAsync(i => i.WorkspaceId == null, cancellationToken)) return;

        var groups = new Dictionary<string, string[]>
        {
            ["Frukt & Grönt"] = ["Tomat", "Gurka", "Sallad", "Paprika", "Lök", "Vitlök", "Rödlök", "Morötter", "Potatis", "Sötpotatis", "Zucchini", "Aubergine", "Blomkål", "Broccoli", "Vitkål", "Spenat", "Champinjoner", "Citron", "Lime", "Ingefära", "Persilja", "Basilika", "Dill", "Avokado"],
            ["Mejeri & Ägg"] = ["Mjölk", "Smör", "Grädde", "Crème fraîche", "Ost", "Mozzarella", "Halloumi", "Ägg", "Yoghurt"],
            ["Kött & Fågel"] = ["Nötfärs", "Kycklingfilé", "Kycklinglårfilé", "Fläskfilé", "Fläskkarré", "Falukorv"],
            ["Fisk & Skaldjur"] = ["Lax", "Laxfilé", "Torskfilé", "Räkor", "Tonfisk på burk"],
            ["Skafferi"] = ["Spaghetti", "Pasta", "Penne", "Ris", "Bulgur", "Nudlar", "Risnudlar", "Tortillabröd", "Bao-bröd", "Mjöl", "Ströbröd", "Olivolja", "Sesamolja", "Soja", "Buljong", "Tomatpuré", "Krossade tomater", "Kokosmjölk"],
            ["Baljväxter & Frön"] = ["Kikärtor", "Röda linser", "Kidneybönor", "Svarta bönor", "Tofu", "Jordnötter"],
            ["Övrigt"] = ["Hoisinsås", "BBQ-sås", "Tacosås", "Pesto", "Ajvar", "Lingonsylt", "Senap", "Salladslök"]
        };

        var items = groups.SelectMany(g => g.Value.Select(name => new IngredientMaster
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = g.Key,
            WorkspaceId = null
        }));
        await context.IngredientMaster.AddRangeAsync(items, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
