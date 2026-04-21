using Eatah.Api.Common;

namespace Eatah.Api.Features.WeeklyPlan;

public static class ClearMealFromDay
{
    public static async Task<IResult> Handle(
        Guid id,
        DayOfWeek dayOfWeek,
        WeeklyPlanService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ClearDayAsync(id, dayOfWeek, cancellationToken);
        return result.ToHttpResult();
    }
}
