using FluentValidation;

namespace Eatah.Api.Features.Meals;

public class CreateMealRequestValidator : AbstractValidator<CreateMealRequest>
{
    public CreateMealRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Maträttens namn är obligatoriskt.")
            .MaximumLength(200).WithMessage("Namnet får vara max 200 tecken.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Ogiltig kategori.");

        RuleFor(x => x.Ingredients)
            .NotEmpty().WithMessage("Minst en ingrediens krävs.");

        RuleForEach(x => x.Ingredients)
            .NotEmpty().WithMessage("Ingrediensen får inte vara tom.")
            .MaximumLength(200).WithMessage("Ingrediensens namn får vara max 200 tecken.");

        RuleFor(x => x.CookingTimeMinutes!.Value)
            .InclusiveBetween(1, 600).WithMessage("Tillagningstiden måste vara mellan 1 och 600 minuter.")
            .When(x => x.CookingTimeMinutes.HasValue);
    }
}

public class UpdateMealRequestValidator : AbstractValidator<UpdateMealRequest>
{
    public UpdateMealRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Maträttens namn är obligatoriskt.")
            .MaximumLength(200).WithMessage("Namnet får vara max 200 tecken.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Ogiltig kategori.");

        RuleFor(x => x.Ingredients)
            .NotEmpty().WithMessage("Minst en ingrediens krävs.");

        RuleForEach(x => x.Ingredients)
            .NotEmpty().WithMessage("Ingrediensen får inte vara tom.")
            .MaximumLength(200).WithMessage("Ingrediensens namn får vara max 200 tecken.");
    }
}
