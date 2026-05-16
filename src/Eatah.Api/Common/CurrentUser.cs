using System.Security.Claims;

namespace Eatah.Api.Common;

/// <summary>
/// Request-scoped current user accessor backed by <see cref="IHttpContextAccessor"/>.
/// Returns the user id from the <c>NameIdentifier</c> claim, or null if not authenticated.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? UserId
    {
        get
        {
            var principal = _accessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true) return null;
            var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }

    public bool IsAuthenticated => UserId is not null;
}
