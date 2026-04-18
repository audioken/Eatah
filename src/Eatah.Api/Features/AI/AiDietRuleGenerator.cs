using System.Text.Json;
using Eatah.Api.Features.DietRules;
using Eatah.Domain.Entities;

namespace Eatah.Api.Features.AI;

public class AiDietRuleGenerator
{
    private const string SystemPrompt = """
        Du är en nutritionsexpert som genererar kostregler för en veckomatplan.
        Svara enbart med JSON enligt följande schema:
        {
          "name": "string",
          "rules": [
            {
              "category": "Meat" | "Fish" | "Vegetarian" | "Vegan",
              "minPerWeek": 0-7,
              "maxPerWeek": 0-7,
              "description": "kort svensk beskrivning"
            }
          ]
        }
        Krav:
        - minPerWeek <= maxPerWeek
        - En regel per kategori (Meat, Fish, Vegetarian, Vegan)
        - Beskrivningar ska vara på svenska, högst 500 tecken.
        """;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly IAiClient _aiClient;
    private readonly IDietProfileRepository _profileRepository;
    private readonly ILogger<AiDietRuleGenerator> _logger;

    public AiDietRuleGenerator(
        IAiClient aiClient,
        IDietProfileRepository profileRepository,
        ILogger<AiDietRuleGenerator> logger)
    {
        _aiClient = aiClient;
        _profileRepository = profileRepository;
        _logger = logger;
    }

    public async Task<DietProfileResponse> GenerateAndSaveAsync(
        GenerateDietProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userPrompt = BuildUserPrompt(request);

        var raw = await _aiClient.CompleteAsync(SystemPrompt, userPrompt, cancellationToken);
        var generated = ParseAndValidate(raw, request.Name);

        var profile = new DietProfile
        {
            Id = Guid.NewGuid(),
            Name = await EnsureUniqueNameAsync(generated.Name, cancellationToken),
            Rules = generated.Rules.Select(r => new DietRule
            {
                Id = Guid.NewGuid(),
                Category = r.Category,
                MinPerWeek = r.MinPerWeek,
                MaxPerWeek = r.MaxPerWeek,
                Description = r.Description
            }).ToList()
        };

        await _profileRepository.AddAsync(profile, cancellationToken);

        return new DietProfileResponse(
            profile.Id,
            profile.Name,
            profile.Rules
                .Select(r => new DietRuleResponse(r.Id, r.Category, r.MinPerWeek, r.MaxPerWeek, r.Description))
                .ToList());
    }

    private static string BuildUserPrompt(GenerateDietProfileRequest request)
    {
        var strictness = Math.Clamp(request.Strictness, 0.0, 1.0);
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? "(ingen ytterligare beskrivning)"
            : request.Description.Trim();

        return $"""
            Generera en kostprofil med namnet "{request.Name}".
            Beskrivning/mål: {description}
            Strikthet: {strictness:F2} (0.0 = mycket flexibel, 1.0 = mycket strikt).
            Returnera JSON enligt schemat.
            """;
    }

    public static AiGeneratedProfile ParseAndValidate(string rawJson, string fallbackName)
    {
        AiGeneratedProfile? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<AiGeneratedProfile>(rawJson, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new AiServiceException("Kunde inte tolka svaret från AI-tjänsten.", ex);
        }

        if (parsed is null || parsed.Rules is null || parsed.Rules.Count == 0)
        {
            throw new AiServiceException("AI-tjänsten returnerade inga kostregler.");
        }

        var name = string.IsNullOrWhiteSpace(parsed.Name) ? fallbackName : parsed.Name.Trim();

        var validatedRules = new List<AiGeneratedRule>();
        var seenCategories = new HashSet<MealCategory>();

        foreach (var rule in parsed.Rules)
        {
            if (!Enum.IsDefined(typeof(MealCategory), rule.Category))
            {
                continue;
            }

            if (!seenCategories.Add(rule.Category))
            {
                continue;
            }

            var min = Math.Clamp(rule.MinPerWeek, 0, 7);
            var max = Math.Clamp(rule.MaxPerWeek, 0, 7);
            if (min > max)
            {
                (min, max) = (max, min);
            }

            var description = string.IsNullOrWhiteSpace(rule.Description)
                ? $"{rule.Category}: {min}–{max} gånger per vecka."
                : rule.Description.Trim();

            if (description.Length > 500)
            {
                description = description[..500];
            }

            validatedRules.Add(new AiGeneratedRule(rule.Category, min, max, description));
        }

        if (validatedRules.Count == 0)
        {
            throw new AiServiceException("AI-tjänsten returnerade inga giltiga kostregler.");
        }

        return new AiGeneratedProfile(name, validatedRules);
    }

    private async Task<string> EnsureUniqueNameAsync(string name, CancellationToken cancellationToken)
    {
        var candidate = name;
        var suffix = 2;
        while (await _profileRepository.ExistsByNameAsync(candidate, cancellationToken))
        {
            candidate = $"{name} ({suffix++})";
            if (suffix > 100)
            {
                candidate = $"{name} ({Guid.NewGuid():N}[..6])";
                break;
            }
        }
        return candidate;
    }
}
