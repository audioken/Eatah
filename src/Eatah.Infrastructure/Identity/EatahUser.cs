using Microsoft.AspNetCore.Identity;

namespace Eatah.Infrastructure.Identity;

/// <summary>
/// Application user. Lives in Infrastructure because it depends on ASP.NET Identity types
/// (Domain must remain dependency-free per architecture rules).
/// </summary>
public class EatahUser : IdentityUser<Guid>
{
    /// <summary>
    /// Public, unique display name used in friend search and UI. Required after email confirmation.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
