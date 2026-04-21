using Eatah.Api.Common;
using FluentValidation;

namespace Eatah.Api.Features.Meals;

public static class UpdateMeal
{
    public static async Task<IResult> Handle(
        Guid id,
        UpdateMealRequest request,
        MealService service,
        IValidator<UpdateMealRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return Result<MealResponse>.Failure(validationError).ToHttpResult();
        }

        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result.ToHttpResult();
    }
}
