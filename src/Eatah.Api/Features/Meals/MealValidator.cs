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
