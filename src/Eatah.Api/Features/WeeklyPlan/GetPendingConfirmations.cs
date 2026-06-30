namespace Eatah.Api.Features.WeeklyPlan;

public static class GetPendingConfirmations
{
    public static async Task<IResult> Handle(MealConfirmationService service, CancellationToken ct)
    {
        var pending = await service.GetPendingAsync(ct);
        return Results.Ok(pending);
    }
}
