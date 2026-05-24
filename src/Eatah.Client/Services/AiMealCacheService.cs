using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// Singleton that holds the most recently AI-generated meal so the result
/// survives page navigation and week switching within the same app session.
/// Cleared on workspace change or after the meal is saved.
/// </summary>
public sealed class AiMealCacheService
{
    public AiGeneratedMealResponse? Meal { get; set; }

    public void Clear() => Meal = null;
}
