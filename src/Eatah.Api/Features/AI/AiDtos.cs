using Eatah.Domain.Entities;

namespace Eatah.Api.Features.AI;

public record GenerateDietProfileRequest(
    string Name,
    string? Description,
    double Strictness);

public record AiGeneratedRule(
    MealCategory Category,
    int MinPerWeek,
    int MaxPerWeek,
    string Description);

public record AiGeneratedProfile(
    string Name,
    List<AiGeneratedRule> Rules);
