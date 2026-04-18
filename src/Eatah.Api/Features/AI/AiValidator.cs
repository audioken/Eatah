using FluentValidation;

namespace Eatah.Api.Features.AI;

public class GenerateDietProfileRequestValidator : AbstractValidator<GenerateDietProfileRequest>
{
    public GenerateDietProfileRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Profilens namn är obligatoriskt.")
            .MaximumLength(100).WithMessage("Namnet får vara max 100 tecken.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Beskrivningen får vara max 500 tecken.");

        RuleFor(x => x.Strictness)
            .InclusiveBetween(0.0, 1.0).WithMessage("Strictness måste vara mellan 0.0 och 1.0.");
    }
}
