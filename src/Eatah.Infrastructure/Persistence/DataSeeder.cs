using Eatah.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(EatahDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Meals.AnyAsync(cancellationToken))
        {
            return;
        }

        var meals = new List<Meal>
        {
            // Kött
            CreateMeal("Spaghetti köttfärssås", MealCategory.Meat, "Spaghetti", "Nötfärs", "Krossade tomater", "Lök", "Vitlök"),
            CreateMeal("Blomkålsris med kyckling", MealCategory.Meat, "Kycklingfilé", "Blomkål", "Curry", "Kokosmjölk", "Lök"),
            CreateMeal("Kålpudding", MealCategory.Meat, "Vitkål", "Nötfärs", "Lök", "Grädde", "Lingonsylt"),
            CreateMeal("Falukorv med potatismos", MealCategory.Meat, "Falukorv", "Potatis", "Mjölk", "Smör", "Senap"),
            CreateMeal("Kycklingcurry med ris", MealCategory.Meat, "Kycklingfilé", "Ris", "Currypasta", "Kokosmjölk", "Paprika"),
            CreateMeal("Köttbullar med potatismos", MealCategory.Meat, "Nötfärs", "Lök", "Ströbröd", "Potatis", "Gräddsås"),
            CreateMeal("Tacos", MealCategory.Meat, "Nötfärs", "Tortillabröd", "Sallad", "Tomat", "Tacosås"),
            CreateMeal("Pannbiff med lök", MealCategory.Meat, "Nötfärs", "Lök", "Potatis", "Gräddsås", "Lingonsylt"),
            CreateMeal("Fläskfilé med grönpepparsås", MealCategory.Meat, "Fläskfilé", "Grönpeppar", "Grädde", "Potatis", "Sallad"),
            CreateMeal("Stekt kycklinglårfilé med ugnsrostade grönsaker", MealCategory.Meat, "Kycklinglårfilé", "Paprika", "Zucchini", "Rödlök", "Olivolja"),
            CreateMeal("Korv Stroganoff", MealCategory.Meat, "Falukorv", "Tomatpuré", "Grädde", "Ris", "Lök"),
            CreateMeal("Pulled pork-burgare", MealCategory.Meat, "Fläskkarré", "Hamburgerbröd", "Coleslaw", "BBQ-sås", "Pickles"),

            // Fisk
            CreateMeal("Laxcarbonara", MealCategory.Fish, "Lax", "Spaghetti", "Ägg", "Parmesan", "Grädde"),
            CreateMeal("Stekt lax med dillsås", MealCategory.Fish, "Laxfilé", "Dill", "Grädde", "Potatis", "Citron"),
            CreateMeal("Fish and chips", MealCategory.Fish, "Torskfilé", "Mjöl", "Potatis", "Ärtor", "Citron"),
            CreateMeal("Räkpasta med vitlök", MealCategory.Fish, "Räkor", "Pasta", "Vitlök", "Chili", "Persilja"),
            CreateMeal("Tonfiskpasta", MealCategory.Fish, "Tonfisk på burk", "Penne", "Crème fraîche", "Majs", "Lök"),
            CreateMeal("Sojagravad lax med nudlar", MealCategory.Fish, "Laxfilé", "Soja", "Sesamolja", "Nudlar", "Salladslök"),
            CreateMeal("Fiskgratäng", MealCategory.Fish, "Torskfilé", "Räkor", "Grädde", "Ost", "Potatis"),

            // Vegetariskt
            CreateMeal("Bao med tofu", MealCategory.Vegetarian, "Bao-bröd", "Tofu", "Hoisinsås", "Vårlök", "Gurka"),
            CreateMeal("Grönsakssoppa", MealCategory.Vegetarian, "Morötter", "Potatis", "Lök", "Buljong", "Selleri"),
            CreateMeal("Spaghetti med tomatsås och buffelmozzarella", MealCategory.Vegetarian, "Spaghetti", "Krossade tomater", "Buffelmozzarella", "Basilika", "Olivolja"),
            CreateMeal("Halloumiburgare", MealCategory.Vegetarian, "Halloumi", "Hamburgerbröd", "Sallad", "Tomat", "Ajvar"),
            CreateMeal("Omelett med ost och svamp", MealCategory.Vegetarian, "Ägg", "Champinjoner", "Ost", "Lök", "Persilja"),
            CreateMeal("Pasta pesto med mozzarella", MealCategory.Vegetarian, "Pasta", "Pesto", "Mozzarella", "Soltorkade tomater", "Ruccola"),
            CreateMeal("Pannkakor", MealCategory.Vegetarian, "Mjöl", "Ägg", "Mjölk", "Smör", "Sylt"),
            CreateMeal("Grillad halloumi med bulgursallad", MealCategory.Vegetarian, "Halloumi", "Bulgur", "Gurka", "Tomat", "Citron"),

            // Veganskt
            CreateMeal("Sötpotatissoppa med vitlöksbröd", MealCategory.Vegan, "Sötpotatis", "Lök", "Vitlök", "Kokosmjölk", "Bröd"),
            CreateMeal("Linssoppa med bröd", MealCategory.Vegan, "Röda linser", "Morot", "Lök", "Spiskummin", "Bröd"),
            CreateMeal("Vegansk pad thai", MealCategory.Vegan, "Risnudlar", "Tofu", "Böngroddar", "Jordnötter", "Lime"),
            CreateMeal("Kikärtsgryta med ris", MealCategory.Vegan, "Kikärtor", "Krossade tomater", "Spenat", "Ris", "Garam masala"),
            CreateMeal("Bönchili", MealCategory.Vegan, "Kidneybönor", "Svarta bönor", "Krossade tomater", "Paprika", "Ris"),
        };

        await context.Meals.AddRangeAsync(meals, cancellationToken);

        if (!await context.DietProfiles.AnyAsync(cancellationToken))
        {
            var defaultProfile = new DietProfile
            {
                Id = Guid.NewGuid(),
                Name = "Livsmedelsverket",
                Rules =
                [
                    new DietRule { Id = Guid.NewGuid(), Category = MealCategory.Fish, MinPerWeek = 2, MaxPerWeek = 3, Description = "Ät fisk 2–3 gånger per vecka." },
                    new DietRule { Id = Guid.NewGuid(), Category = MealCategory.Meat, MinPerWeek = 0, MaxPerWeek = 3, Description = "Begränsa rött och processat kött." },
                    new DietRule { Id = Guid.NewGuid(), Category = MealCategory.Vegetarian, MinPerWeek = 2, MaxPerWeek = 7, Description = "Ät vegetariskt minst 2 gånger per vecka." }
                ]
            };

            await context.DietProfiles.AddAsync(defaultProfile, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static Meal CreateMeal(string name, MealCategory category, params string[] ingredients)
    {
        return new Meal
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category,
            Ingredients = ingredients
                .Select(i => new Ingredient { Id = Guid.NewGuid(), Name = i })
                .ToList()
        };
    }
}
