using Microsoft.AspNetCore.Components;

namespace Eatah.Client.Services;

/// <summary>
/// Lets pages set the three slots of the global <c>AppHeader</c> rendered by
/// <c>MainLayout</c>. Pages call <see cref="Set"/> in <c>OnInitialized</c> and
/// <see cref="Clear"/> on dispose to avoid leaking content between routes.
/// </summary>
public sealed class HeaderState
{
    public RenderFragment? Left { get; private set; }
    public RenderFragment? Center { get; private set; }
    public RenderFragment? Right { get; private set; }

    public event Action? OnChange;

    public void Set(RenderFragment? center = null, RenderFragment? left = null, RenderFragment? right = null)
    {
        Center = center;
        Left = left;
        Right = right;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        Left = Center = Right = null;
        OnChange?.Invoke();
    }
}
