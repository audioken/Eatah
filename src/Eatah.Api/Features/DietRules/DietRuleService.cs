using Eatah.Api.Common;
using Eatah.Api.Features.WeeklyPlan;
using Eatah.Domain.Entities;

namespace Eatah.Api.Features.DietRules;

public class DietRuleService
{
    private readonly IDietProfileRepository _profileRepository;
    private readonly IWeeklyPlanRepository _weeklyPlanRepository;
    private readonly IDietRuleEvaluator _evaluator;

    public DietRuleService(
        IDietProfileRepository profileRepository,
        IWeeklyPlanRepository weeklyPlanRepository,
        IDietRuleEvaluator evaluator)
    {
        _profileRepository = profileRepository;
        _weeklyPlanRepository = weeklyPlanRepository;
        _evaluator = evaluator;
    }

    public async Task<List<DietProfileResponse>> GetAllProfilesAsync(CancellationToken cancellationToken)
    {
        var profiles = await _profileRepository.GetAllAsync(cancellationToken);
        return profiles.Select(ToResponse).ToList();
    }

    public async Task<Result<DietProfileResponse>> GetProfileAsync(Guid id, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByIdAsync(id, cancellationToken);
        return profile is null
            ? DietProfileErrors.NotFound(id)
            : ToResponse(profile);
    }

    public async Task<Result<DietEvaluationResponse>> EvaluateAsync(
        Guid weeklyPlanId,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var plan = await _weeklyPlanRepository.GetByIdAsync(weeklyPlanId, cancellationToken);
        if (plan is null)
        {
            return Error.NotFound(ErrorCodes.WeeklyPlanNotFound, $"Weekly plan with id {weeklyPlanId} was not found.");
        }

        var profile = await _profileRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return DietProfileErrors.NotFound(profileId);
        }

        var evaluation = _evaluator.Evaluate(plan, profile);

        return new DietEvaluationResponse(
            evaluation.OverallScore,
            evaluation.RuleResults
                .Select(r => new RuleResultResponse(
                    r.RuleName,
                    r.Category,
                    r.IsMet,
                    r.Actual,
                    r.Min,
                    r.Max,
                    r.Score,
                    r.Message))
                .ToList());
    }

    internal static DietProfileResponse ToResponse(DietProfile profile)
    {
        return new DietProfileResponse(
            profile.Id,
            profile.Name,
            profile.Rules
                .Select(r => new DietRuleResponse(r.Id, r.Category, r.MinPerWeek, r.MaxPerWeek, r.Description))
                .ToList());
    }
}

internal static class DietProfileErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(ErrorCodes.DietProfileNotFound, $"Diet profile with id {id} was not found.");
}
