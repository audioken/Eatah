namespace Eatah.Api.Common;

/// <summary>
/// Represents a structured, machine-readable error returned from a service operation.
/// Maps to a standardized RFC 7807 ProblemDetails response with an additional
/// <c>errorCode</c> extension for predictable client-side handling.
/// </summary>
public sealed record Error(
    string Code,
    string Message,
    int StatusCode,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null)
{
    public static Error NotFound(string code, string message) =>
        new(code, message, StatusCodes.Status404NotFound);

    public static Error Conflict(string code, string message) =>
        new(code, message, StatusCodes.Status409Conflict);

    public static Error Forbidden(string code, string message) =>
        new(code, message, StatusCodes.Status403Forbidden);

    public static Error Unauthorized(string code, string message) =>
        new(code, message, StatusCodes.Status401Unauthorized);

    public static Error Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new(ErrorCodes.ValidationError, "One or more validation errors occurred.",
            StatusCodes.Status400BadRequest, errors);

    public static Error BadRequest(string code, string message) =>
        new(code, message, StatusCodes.Status400BadRequest);

    public static Error Upstream(string code, string message) =>
        new(code, message, StatusCodes.Status502BadGateway);

    public static Error Unexpected(string code, string message) =>
        new(code, message, StatusCodes.Status500InternalServerError);
}
