namespace Eatah.Api.Features.WeeklyPlan;

public static class GetWeeklyPlanByWeek
{
    public static async Task<IResult> Handle(
        int year,
        int weekNumber,
        WeeklyPlanService service,
        CancellationToken cancellationToken)
    {
        var plan = await service.GetOrCreateByWeekAsync(year, weekNumber, cancellationToken);
        return Results.Ok(plan);
    }
}
