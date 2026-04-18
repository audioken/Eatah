using Eatah.Domain.Entities;

namespace Eatah.Api.Features.DietRules;

public record DietRuleResponse(
    Guid Id,
    MealCategory Category,
    int MinPerWeek,
    int MaxPerWeek,
    string Description);

public record DietProfileResponse(
    Guid Id,
    string Name,
    List<DietRuleResponse> Rules);

public record RuleResultResponse(
    string RuleName,
    MealCategory Category,
    bool IsMet,
    int Actual,
    int Min,
    int Max,
    double Score,
    string Message);

public record DietEvaluationResponse(
    double OverallScore,
    List<RuleResultResponse> RuleResults);
