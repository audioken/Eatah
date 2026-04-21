using Eatah.Api.Common;
using FluentValidation;

namespace Eatah.Api.Features.AI;

public static class GenerateAiMeal
{
    public static async Task<IResult> Handle(
        GenerateMealRequest request,
        AiMealGenerator generator,
        IValidator<GenerateMealRequest> validator,
        ILogger<AiMealGenerator> logger,
        CancellationToken cancellationToken)
    {
        var validationError = await validator.ValidateRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return Result<AiGeneratedMealResponse>.Failure(validationError).ToHttpResult();
        }

        try
        {
            var meal = await generator.GenerateAsync(request, cancellationToken);
            return Result<AiGeneratedMealResponse>.Success(meal).ToHttpResult();
        }
        catch (AiServiceException ex)
        {
            logger.LogWarning(ex, "AI generation of meal failed.");
            var error = Error.Upstream(ex.Code, ex.Message);
            return Result<AiGeneratedMealResponse>.Failure(error).ToHttpResult();
        }
    }
}
