using Microsoft.AspNetCore.Mvc;

namespace Eatah.Api.Common;

/// <summary>
/// Maps a <see cref="Result{T}"/> or <see cref="Result"/> into an
/// <see cref="IResult"/> with a consistent ProblemDetails error envelope.
/// All error responses include an <c>errorCode</c> extension that clients
/// can switch on for localized UI handling.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
        => result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);

    public static IResult ToCreatedResult<T>(this Result<T> result, Func<T, string> locationFactory)
        => result.IsSuccess
            ? Results.Created(locationFactory(result.Value!), result.Value)
            : ToProblem(result.Error!);

    public static IResult ToNoContentResult(this Result result)
        => result.IsSuccess
            ? Results.NoContent()
            : ToProblem(result.Error!);

    public static IResult ToNoContentResult<T>(this Result<T> result)
        => result.IsSuccess
            ? Results.NoContent()
            : ToProblem(result.Error!);

    /// <summary>Converts a standalone <see cref="Error"/> to a ProblemDetails response.</summary>
    public static IResult ToHttpResult(this Error error) => ToProblem(error);

    private static IResult ToProblem(Error error)
    {
        if (error.ValidationErrors is { Count: > 0 })
        {
            // Use built-in helper but enrich with errorCode extension.
            var validationProblem = new HttpValidationProblemDetails(
                error.ValidationErrors.ToDictionary(kv => kv.Key, kv => kv.Value))
            {
                Status = error.StatusCode,
                Title = error.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            };
            validationProblem.Extensions["errorCode"] = error.Code;
            return Results.Problem(validationProblem);
        }

        var problem = new ProblemDetails
        {
            Status = error.StatusCode,
            Title = error.Message,
            Detail = error.Message,
            Type = "https://tools.ietf.org/html/rfc7807",
            Extensions = { ["errorCode"] = error.Code }
        };
        return Results.Problem(problem);
    }
}
