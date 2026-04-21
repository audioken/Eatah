using FluentValidation;
using FluentValidation.Results;

namespace Eatah.Api.Common;

public static class ValidationExtensions
{
    /// <summary>
    /// Runs the validator and returns a failure <see cref="Result{T}"/>
    /// containing a structured <see cref="Error"/> if validation fails.
    /// Returns <c>null</c> when the request is valid.
    /// </summary>
    public static async Task<Error?> ValidateRequestAsync<T>(
        this IValidator<T> validator,
        T request,
        CancellationToken cancellationToken)
    {
        ValidationResult validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.IsValid)
        {
            return null;
        }

        var errors = validation.Errors
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        return Error.Validation(errors);
    }
}
