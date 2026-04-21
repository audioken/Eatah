using Eatah.Api.Common;

namespace Eatah.Api.Features.DietRules;

public static class EvaluateWeeklyPlan
{
    public static async Task<IResult> Handle(
        Guid id,
        Guid profileId,
        DietRuleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.EvaluateAsync(id, profileId, cancellationToken);
        return result.ToHttpResult();
    }
}
