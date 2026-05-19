using Eatah.Api.Common;
using FluentValidation;

namespace Eatah.Api.Features.DietRules;

public static class CreateDietProfile
{
    public static async Task<IResult> Handle(
        CreateDietProfileRequest request,
        IValidator<CreateDietProfileRequest> validator,
        DietRuleService service,
        CancellationToken ct)
    {
        var validationError = await validator.ValidateRequestAsync(request, ct);
        if (validationError is not null)
            return Result<DietProfileResponse>.Failure(validationError).ToHttpResult();

        var result = await service.CreateAsync(request, ct);
        return result.ToCreatedResult(r => $"/api/dietprofiles/{r.Id}");
    }
}

public class CreateDietProfileValidator : AbstractValidator<CreateDietProfileRequest>
{
    public CreateDietProfileValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Profile name is required.")
            .MaximumLength(200).WithMessage("Profile name must be at most 200 characters.");

        RuleFor(x => x.Rules)
            .NotEmpty().WithMessage("At least one rule is required.");

        RuleForEach(x => x.Rules).ChildRules(rule =>
        {
            rule.RuleFor(r => r.MinPerWeek)
                .InclusiveBetween(0, 7).WithMessage("MinPerWeek must be between 0 and 7.");
            rule.RuleFor(r => r.MaxPerWeek)
                .InclusiveBetween(0, 7).WithMessage("MaxPerWeek must be between 0 and 7.")
                .GreaterThanOrEqualTo(r => r.MinPerWeek)
                .WithMessage("MaxPerWeek must be greater than or equal to MinPerWeek.");
        });
    }
}
