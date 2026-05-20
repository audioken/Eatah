using Eatah.Api.Common;

namespace Eatah.Api.Features.WeeklyPlan;

public static class UpdateWeeklyPlanDietProfile
{
    public static async Task<IResult> Handle(
        Guid id,
        UpdateWeeklyPlanDietProfileRequest request,
        WeeklyPlanService service,
        CancellationToken ct)
    {
        var result = await service.UpdateDietProfileAsync(id, request, ct);
        return result.ToHttpResult();
    }
}
