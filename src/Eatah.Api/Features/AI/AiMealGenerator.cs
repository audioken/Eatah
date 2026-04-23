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
        var userPrompt = await BuildUserPromptAsync(request, cancellationToken);
        var raw = await _aiClient.CompleteAsync(SystemPrompt, userPrompt, cancellationToken);
        return ParseAndValidate(raw);
    }

    private async Task<string> BuildUserPromptAsync(GenerateMealRequest request, CancellationToken ct)
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

        if (request.DietProfileId is Guid profileId)
        {
            var profile = await _profileRepository.GetByIdAsync(profileId, ct);
            if (profile is not null && profile.Rules.Count > 0)
            {
                sb.AppendLine($"Aktiv kostprofil: \"{profile.Name}\". Regler per vecka:");
                foreach (var rule in profile.Rules)
                {
                    sb.AppendLine($"- {rule.Category}: {rule.MinPerWeek}-{rule.MaxPerWeek} ggr ({rule.Description})");
                }
            }
        }

        if (request.WeeklyPlanId is Guid planId)
        {
            var plan = await _planRepository.GetByIdAsync(planId, ct);
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
