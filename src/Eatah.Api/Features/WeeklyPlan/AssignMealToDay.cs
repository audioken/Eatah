using Eatah.Api.Common;
using FluentValidation;

namespace Eatah.Api.Features.WeeklyPlan;

public static class AssignMealToDay
{
    public static async Task<IResult> Handle(
        Guid id,
        DayOfWeek dayOfWeek,
        AssignMealRequest request,
        WeeklyPlanService service,
        IValidator<AssignMealRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return Result<WeeklyPlanResponse>.Failure(validationError).ToHttpResult();
        }

        var result = await service.AssignMealAsync(id, dayOfWeek, request.MealId, cancellationToken);
        return result.ToHttpResult();
    }
}
