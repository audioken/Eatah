using Eatah.Api.Common;

namespace Eatah.Api.Features.Meals;

public static class DeleteMeal
{
    public static async Task<IResult> Handle(Guid id, MealService service, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        return result.ToNoContentResult();
    }
}
