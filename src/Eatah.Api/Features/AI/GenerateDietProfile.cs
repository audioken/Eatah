using Eatah.Api.Common;
using Eatah.Api.Features.DietRules;
using FluentValidation;

namespace Eatah.Api.Features.AI;

public static class GenerateDietProfile
{
    public static async Task<IResult> Handle(
        GenerateDietProfileRequest request,
        AiDietRuleGenerator generator,
        IValidator<GenerateDietProfileRequest> validator,
        ILogger<AiDietRuleGenerator> logger,
        CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return Result<DietProfileResponse>.Failure(validationError).ToHttpResult();
        }

        try
        {
            var profile = await generator.GenerateAndSaveAsync(request, cancellationToken);
            return Result<DietProfileResponse>.Success(profile)
                .ToCreatedResult(value => $"/api/dietprofiles/{value.Id}");
        }
        catch (AiServiceException ex)
        {
            logger.LogWarning(ex, "AI generation of diet profile failed.");
            var error = Error.Upstream(ex.Code, ex.Message);
            return Result<DietProfileResponse>.Failure(error).ToHttpResult();
        }
    }
}
