using FluentValidation;

namespace Eatah.Api.Features.Meals;

public class CreateMealRequestValidator : AbstractValidator<CreateMealRequest>
{
    public CreateMealRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Meal name is required.")
            .MaximumLength(200).WithMessage("Name must be at most 200 characters.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid meal category.");

        RuleFor(x => x.Ingredients)
            .NotEmpty().WithMessage("At least one ingredient is required.");

        RuleForEach(x => x.Ingredients)
            .NotEmpty().WithMessage("Ingredient name cannot be empty.")
            .MaximumLength(200).WithMessage("Ingredient name must be at most 200 characters.");

        RuleFor(x => x.CookingTimeMinutes!.Value)
            .InclusiveBetween(1, 600).WithMessage("Cooking time must be between 1 and 600 minutes.")
            .When(x => x.CookingTimeMinutes.HasValue);
    }
}

public class UpdateMealRequestValidator : AbstractValidator<UpdateMealRequest>
{
    public UpdateMealRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Meal name is required.")
            .MaximumLength(200).WithMessage("Name must be at most 200 characters.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid meal category.");

        RuleFor(x => x.Ingredients)
            .NotEmpty().WithMessage("At least one ingredient is required.");

        RuleForEach(x => x.Ingredients)
            .NotEmpty().WithMessage("Ingredient name cannot be empty.")
            .MaximumLength(200).WithMessage("Ingredient name must be at most 200 characters.");
    }
}
