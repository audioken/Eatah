using FluentValidation;

namespace Eatah.Api.Features.WeeklyPlan;

public class CreateWeeklyPlanRequestValidator : AbstractValidator<CreateWeeklyPlanRequest>
{
    public CreateWeeklyPlanRequestValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage("Year must be between 2000 and 2100.");

        RuleFor(x => x.WeekNumber)
            .InclusiveBetween(1, 53).WithMessage("Week number must be between 1 and 53.");
    }
}

public class AssignMealRequestValidator : AbstractValidator<AssignMealRequest>
{
    public AssignMealRequestValidator()
    {
        RuleFor(x => x.MealId)
            .NotEmpty().WithMessage("MealId is required.");
    }
}

public class RandomizeWeeklyPlanRequestValidator : AbstractValidator<RandomizeWeeklyPlanRequest>
{
    public RandomizeWeeklyPlanRequestValidator()
    {
        RuleFor(x => x.Strictness)
            .InclusiveBetween(0.0, 1.0).WithMessage("Strictness must be between 0.0 and 1.0.");
    }
}

public class RandomizeDayRequestValidator : AbstractValidator<RandomizeDayRequest>
{
    public RandomizeDayRequestValidator()
    {
        RuleFor(x => x.Strictness)
            .InclusiveBetween(0.0, 1.0).WithMessage("Strictness must be between 0.0 and 1.0.");
    }
}
