using Eatah.Api.Common;
using FluentValidation;

namespace Eatah.Api.Features.WeeklyPlan;

public static class RandomizeDay
{
    public static async Task<IResult> Handle(
        Guid id,
        DayOfWeek dayOfWeek,
        RandomizeDayRequest request,
        WeeklyPlanService service,
        IValidator<RandomizeDayRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return Result<WeeklyPlanResponse>.Failure(validationError).ToHttpResult();
        }

        var result = await service.RandomizeDayAsync(id, dayOfWeek, request, cancellationToken);
        return result.ToHttpResult();
    }
}
