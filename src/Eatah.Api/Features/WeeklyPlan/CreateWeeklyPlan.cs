using Eatah.Api.Common;
using FluentValidation;

namespace Eatah.Api.Features.WeeklyPlan;

public static class CreateWeeklyPlan
{
    public static async Task<IResult> Handle(
        CreateWeeklyPlanRequest request,
        WeeklyPlanService service,
        IValidator<CreateWeeklyPlanRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return Result<WeeklyPlanResponse>.Failure(validationError).ToHttpResult();
        }

        var result = await service.CreateAsync(request, cancellationToken);
        return result.ToCreatedResult(value => $"/api/weeklyplans/{value.Id}");
    }
}
