using Eatah.Api.Common;
using FluentValidation;

namespace Eatah.Api.Features.Meals;

public static class CreateMeal
{
    public static async Task<IResult> Handle(
        CreateMealRequest request,
        MealService service,
        IValidator<CreateMealRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return Result<MealResponse>.Failure(validationError).ToHttpResult();
        }

        var result = await service.CreateAsync(request, cancellationToken);
        return result.ToCreatedResult(value => $"/api/meals/{value.Id}");
    }
}
