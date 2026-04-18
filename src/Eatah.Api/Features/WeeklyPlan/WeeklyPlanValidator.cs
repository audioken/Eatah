using FluentValidation;

namespace Eatah.Api.Features.WeeklyPlan;

public class CreateWeeklyPlanRequestValidator : AbstractValidator<CreateWeeklyPlanRequest>
{
    public CreateWeeklyPlanRequestValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage("Året måste vara mellan 2000 och 2100.");

        RuleFor(x => x.WeekNumber)
            .InclusiveBetween(1, 53).WithMessage("Veckonumret måste vara mellan 1 och 53.");
    }
}

public class AssignMealRequestValidator : AbstractValidator<AssignMealRequest>
{
    public AssignMealRequestValidator()
    {
        RuleFor(x => x.MealId)
            .NotEmpty().WithMessage("MealId är obligatoriskt.");
    }
}

public class RandomizeWeeklyPlanRequestValidator : AbstractValidator<RandomizeWeeklyPlanRequest>
{
    public RandomizeWeeklyPlanRequestValidator()
    {
        RuleFor(x => x.Strictness)
            .InclusiveBetween(0.0, 1.0).WithMessage("Strictness måste vara mellan 0.0 och 1.0.");
    }
}

public class RandomizeDayRequestValidator : AbstractValidator<RandomizeDayRequest>
{
    public RandomizeDayRequestValidator()
    {
        RuleFor(x => x.Strictness)
            .InclusiveBetween(0.0, 1.0).WithMessage("Strictness måste vara mellan 0.0 och 1.0.");
    }
}
