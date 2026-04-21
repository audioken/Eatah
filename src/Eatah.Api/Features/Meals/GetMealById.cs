using Eatah.Api.Common;

namespace Eatah.Api.Features.Meals;

public static class GetMealById
{
    public static async Task<IResult> Handle(Guid id, MealService service, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result.ToHttpResult();
    }
}
