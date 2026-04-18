using Eatah.Api.Features.WeeklyPlan;
using Eatah.Domain.Entities;

namespace Eatah.Api.Features.DietRules;

public class DietProfileNotFoundException : Exception
{
    public DietProfileNotFoundException(Guid id)
        : base($"Kostprofil med id {id} hittades inte.")
    {
    }
}

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

    public async Task<DietProfileResponse?> GetProfileAsync(Guid id, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByIdAsync(id, cancellationToken);
        return profile is null ? null : ToResponse(profile);
    }

    public async Task<DietEvaluationResponse> EvaluateAsync(
        Guid weeklyPlanId,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var plan = await _weeklyPlanRepository.GetByIdAsync(weeklyPlanId, cancellationToken)
            ?? throw new WeeklyPlanNotFoundException(weeklyPlanId);

        var profile = await _profileRepository.GetByIdAsync(profileId, cancellationToken)
            ?? throw new DietProfileNotFoundException(profileId);

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

    private static DietProfileResponse ToResponse(DietProfile profile)
    {
        return new DietProfileResponse(
            profile.Id,
            profile.Name,
            profile.Rules
                .Select(r => new DietRuleResponse(r.Id, r.Category, r.MinPerWeek, r.MaxPerWeek, r.Description))
                .ToList());
    }
}
