namespace Eatah.Api.Features.Meals;

public static class GetAllMeals
{
    public static async Task<IResult> Handle(MealService service, CancellationToken cancellationToken)
    {
        var meals = await service.GetAllAsync(cancellationToken);
        return Results.Ok(meals);
    }
}
