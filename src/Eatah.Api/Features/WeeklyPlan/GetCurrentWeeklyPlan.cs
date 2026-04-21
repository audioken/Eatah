namespace Eatah.Api.Features.WeeklyPlan;

public static class GetCurrentWeeklyPlan
{
    public static async Task<IResult> Handle(WeeklyPlanService service, CancellationToken cancellationToken)
    {
        var plan = await service.GetCurrentAsync(cancellationToken);
        return Results.Ok(plan);
    }
}
