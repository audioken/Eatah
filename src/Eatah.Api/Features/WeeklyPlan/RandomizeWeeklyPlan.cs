using Eatah.Api.Common;
using FluentValidation;

namespace Eatah.Api.Features.WeeklyPlan;

public static class RandomizeWeeklyPlan
{
    public static async Task<IResult> Handle(
        Guid id,
        RandomizeWeeklyPlanRequest request,
        WeeklyPlanService service,
        IValidator<RandomizeWeeklyPlanRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return Result<WeeklyPlanResponse>.Failure(validationError).ToHttpResult();
        }

        var result = await service.RandomizeAsync(id, request, cancellationToken);
        return result.ToHttpResult();
    }
}
