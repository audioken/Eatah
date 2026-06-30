using Eatah.Api.Common;

namespace Eatah.Api.Features.WeeklyPlan;

public static class ConfirmMeals
{
    public static async Task<IResult> Handle(
        ConfirmMealsRequest request,
        MealConfirmationService service,
        CancellationToken ct)
    {
        var result = await service.ConfirmAsync(request.Confirmations, ct);
        return result.ToNoContentResult();
    }
}
