using FluentValidation;

namespace Eatah.Api.Features.AI;

public class GenerateDietProfileRequestValidator : AbstractValidator<GenerateDietProfileRequest>
{
    public GenerateDietProfileRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Profile name is required.")
            .MaximumLength(100).WithMessage("Name must be at most 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must be at most 500 characters.");
    }
}

public class GenerateMealRequestValidator : AbstractValidator<GenerateMealRequest>
{
    public GenerateMealRequestValidator()
    {
        RuleFor(x => x.Category!.Value)
            .IsInEnum().WithMessage("Invalid meal category.")
            .When(x => x.Category.HasValue);
    }
}
