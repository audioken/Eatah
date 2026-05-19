using System.Text;
using System.Text.Json;
using Eatah.Api.Features.DietRules;
using Eatah.Api.Features.WeeklyPlan;
using Eatah.Domain.Entities;

namespace Eatah.Api.Features.AI;

public class AiMealGenerator
{
    private const string SystemPrompt = """
        Du är en kock som föreslår en enskild maträtt för en veckomatplan.
        Svara enbart med JSON enligt följande schema:
        {
          "name": "string",
          "category": "Meat" | "Poultry" | "Fish" | "Vegetarian" | "Vegan",
          "cookingTimeMinutes": number | null,
          "ingredients": ["string", "string", ...]
        }
        Krav:
        - name: kort, beskrivande svenskt namn på rätten.
        - category: en av de fyra kategorierna.
        - cookingTimeMinutes: ungefärlig total tillagningstid i minuter (1-600), eller null.
        - ingredients: huvudingredienser, 2-12 stycken, korta svenska namn (utan mängder).
          Lista INTE vanliga basvaror och kryddor som salt, peppar, olja, olivolja, vatten, socker – dessa förutsätts alltid finnas hemma.
        Om en kostprofil och tillåtna kategorier anges MÅSTE du välja en kategori från den listan – inga undantag.
        """;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly IAiClient _aiClient;
    private readonly IDietProfileRepository _profileRepository;
    private readonly IWeeklyPlanRepository _planRepository;
    private readonly ILogger<AiMealGenerator> _logger;

    public AiMealGenerator(
        IAiClient aiClient,
        IDietProfileRepository profileRepository,
        IWeeklyPlanRepository planRepository,
        ILogger<AiMealGenerator> logger)
    {
        _aiClient = aiClient;
        _profileRepository = profileRepository;
        _planRepository = planRepository;
        _logger = logger;
    }

    public async Task<AiGeneratedMealResponse> GenerateAsync(
        GenerateMealRequest request,
        CancellationToken cancellationToken)
    {
        DietProfile? profile = null;
        if (request.DietProfileId is Guid profileId)
        {
            profile = await _profileRepository.GetByIdAsync(profileId, cancellationToken);
        }

        Eatah.Domain.Entities.WeeklyPlan? plan = null;
        if (request.WeeklyPlanId is Guid planId)
        {
            plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        }

        var allowedCategories = ComputeAllowedCategories(profile, plan, request.TargetDay);

        // If the caller asked for a specific category, it must also be allowed by the profile.
        if (request.Category.HasValue && allowedCategories is not null && !allowedCategories.Contains(request.Category.Value))
        {
            throw new AiServiceException(
                Common.ErrorCodes.AiInvalidResponse,
                $"Kategori {request.Category.Value} är inte tillåten enligt kostprofilen för denna vecka.");
        }

        if (allowedCategories is { Count: 0 })
        {
            throw new AiServiceException(
                Common.ErrorCodes.AiInvalidResponse,
                "Ingen kategori är tillåten enligt kostprofilen och redan planerade dagar.");
        }

        var userPrompt = BuildUserPrompt(request, profile, plan, allowedCategories, retryHint: null);
        var raw = await _aiClient.CompleteAsync(SystemPrompt, userPrompt, cancellationToken);
        var parsed = ParseAndValidate(raw);

        if (allowedCategories is not null && !allowedCategories.Contains(parsed.Category))
        {
            _logger.LogInformation(
                "AI returned disallowed category {Category}. Retrying with stricter prompt. Allowed: {Allowed}",
                parsed.Category, string.Join(",", allowedCategories));

            var retryPrompt = BuildUserPrompt(request, profile, plan, allowedCategories,
                retryHint: $"Ditt förra förslag var {parsed.Category} vilket inte är tillåtet. Välj STRIKT en av: {string.Join(", ", allowedCategories)}.");
            var retryRaw = await _aiClient.CompleteAsync(SystemPrompt, retryPrompt, cancellationToken);
            parsed = ParseAndValidate(retryRaw);

            if (!allowedCategories.Contains(parsed.Category))
            {
                throw new AiServiceException(
                    Common.ErrorCodes.AiInvalidResponse,
                    $"AI returnerade kategori {parsed.Category} som inte är tillåten enligt kostprofilen.");
            }
        }

        return parsed;
    }

    /// <summary>
    /// Returns the categories that can still be added to the plan without violating
    /// the profile. Returns null when no profile is active (anything goes).
    /// A category is allowed iff it has a rule with MaxPerWeek > 0 and the current
    /// count (excluding the target day) is still below that max.
    /// </summary>
    private static IReadOnlySet<MealCategory>? ComputeAllowedCategories(
        DietProfile? profile,
        Eatah.Domain.Entities.WeeklyPlan? plan,
        DayOfWeek? targetDay)
    {
        if (profile is null || profile.Rules.Count == 0)
            return null;

        var rulesByCategory = profile.Rules.ToDictionary(r => r.Category);
        var currentCounts = new Dictionary<MealCategory, int>();
        if (plan is not null)
        {
            foreach (var day in plan.Days)
            {
                if (targetDay.HasValue && day.DayOfWeek == targetDay.Value) continue;
                if (day.Meal is null) continue;
                // Skip meals whose category isn't allowed by the current profile so
                // a profile switch mid-week doesn't eat into the new budget.
                if (!rulesByCategory.TryGetValue(day.Meal.Category, out var rule) || rule.MaxPerWeek <= 0) continue;
                currentCounts[day.Meal.Category] = currentCounts.GetValueOrDefault(day.Meal.Category, 0) + 1;
            }
        }

        var allowed = new HashSet<MealCategory>();
        foreach (var rule in profile.Rules)
        {
            if (rule.MaxPerWeek <= 0) continue;
            if (currentCounts.GetValueOrDefault(rule.Category, 0) >= rule.MaxPerWeek) continue;
            allowed.Add(rule.Category);
        }

        return allowed;
    }

    private static string BuildUserPrompt(
        GenerateMealRequest request,
        DietProfile? profile,
        Eatah.Domain.Entities.WeeklyPlan? plan,
        IReadOnlySet<MealCategory>? allowedCategories,
        string? retryHint)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Föreslå EN maträtt enligt schemat.");

        if (request.Category.HasValue)
        {
            sb.AppendLine($"Önskad kategori: {request.Category.Value}.");
        }

        if (request.TargetDay.HasValue)
        {
            sb.AppendLine($"Måltiden ska serveras på en {request.TargetDay.Value}.");
        }

        if (profile is not null && profile.Rules.Count > 0)
        {
            sb.AppendLine($"Aktiv kostprofil: \"{profile.Name}\". Regler per vecka:");
            foreach (var rule in profile.Rules)
            {
                sb.AppendLine($"- {rule.Category}: {rule.MinPerWeek}-{rule.MaxPerWeek} ggr ({rule.Description})");
            }
        }

        if (allowedCategories is not null)
        {
            sb.AppendLine(allowedCategories.Count > 0
                ? $"TILLÅTNA kategorier (välj STRIKT en av dessa): {string.Join(", ", allowedCategories)}."
                : "Inga kategorier är tillåtna.");
        }

        if (plan is not null)
        {
            var assigned = plan.Days
                .Where(d => d.Meal is not null)
                .Select(d => $"{d.DayOfWeek}: {d.Meal!.Name} ({d.Meal.Category})")
                .ToList();
            if (assigned.Count > 0)
            {
                sb.AppendLine("Redan planerade dagar denna vecka:");
                foreach (var line in assigned)
                {
                    sb.AppendLine($"- {line}");
                }
                sb.AppendLine("Föreslå gärna något som kompletterar variationen.");
            }
        }

        if (!string.IsNullOrWhiteSpace(retryHint))
        {
            sb.AppendLine(retryHint);
        }

        sb.Append("Returnera JSON enligt schemat utan extra text.");
        return sb.ToString();
    }

    public static AiGeneratedMealResponse ParseAndValidate(string rawJson)
    {
        AiGeneratedMealResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<AiGeneratedMealResponse>(rawJson, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new AiServiceException(Common.ErrorCodes.AiInvalidResponse, "Could not parse AI service response.", ex);
        }

        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Name))
        {
            throw new AiServiceException(Common.ErrorCodes.AiInvalidResponse, "AI service did not return a valid meal name.");
        }

        if (!Enum.IsDefined(typeof(MealCategory), parsed.Category))
        {
            throw new AiServiceException(Common.ErrorCodes.AiInvalidResponse, "AI service returned an unknown meal category.");
        }

        var ingredients = (parsed.Ingredients ?? new List<string>())
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim())
            .Where(i => i.Length <= 200)
            .Take(20)
            .ToList();

        if (ingredients.Count == 0)
        {
            throw new AiServiceException(Common.ErrorCodes.AiInvalidResponse, "AI service returned no ingredients.");
        }

        int? cookingTime = parsed.CookingTimeMinutes;
        if (cookingTime.HasValue && (cookingTime.Value < 1 || cookingTime.Value > 600))
        {
            cookingTime = null;
        }

        var name = parsed.Name.Trim();
        if (name.Length > 200)
        {
            name = name[..200];
        }

        return new AiGeneratedMealResponse(name, parsed.Category, cookingTime, ingredients);
    }
}
