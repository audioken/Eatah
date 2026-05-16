using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// In-memory cache of the currently authenticated user. Single source of truth
/// the UI subscribes to via <see cref="OnChange"/>. The actual session lives in
/// the auth cookie, which <see cref="ApiClient"/> carries automatically.
/// </summary>
public class AuthState
{
    private readonly ApiClient _api;
    private UserResponse? _currentUser;
    private bool _initialized;

    public AuthState(ApiClient api)
    {
        _api = api;
    }

    public UserResponse? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser is not null;
    public bool IsInitialized => _initialized;

    public event Action? OnChange;

    /// <summary>
    /// Calls <c>GET /api/auth/me</c> to discover whether the persisted cookie still represents
    /// a valid session. Safe to call multiple times; cheap after the first call.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        try
        {
            _currentUser = await _api.GetMeAsync(ct);
        }
        catch
        {
            _currentUser = null;
        }
        _initialized = true;
        OnChange?.Invoke();
    }

    public async Task<UserResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _api.LoginAsync(request, ct)
            ?? throw new InvalidOperationException("Login response was empty.");
        _currentUser = user;
        _initialized = true;
        OnChange?.Invoke();
        return user;
    }

    public async Task<UserResponse> ConfirmAndSignInAsync(ConfirmEmailRequest request, CancellationToken ct = default)
    {
        var user = await _api.ConfirmEmailAsync(request, ct)
            ?? throw new InvalidOperationException("Confirmation response was empty.");
        _currentUser = user;
        _initialized = true;
        OnChange?.Invoke();
        return user;
    }

    public async Task<UserResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _api.ResetPasswordAsync(request, ct)
            ?? throw new InvalidOperationException("Reset response was empty.");
        _currentUser = user;
        _initialized = true;
        OnChange?.Invoke();
        return user;
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try
        {
            await _api.LogoutAsync(ct);
        }
        catch
        {
            // Even if the server-side logout fails (network etc.) we drop the local user.
        }
        _currentUser = null;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Called by <see cref="ApiClient"/>-aware code paths when a request unexpectedly returns 401,
    /// indicating the cookie has expired or been revoked.
    /// </summary>
    public void NotifySessionExpired()
    {
        if (_currentUser is null) return;
        _currentUser = null;
        OnChange?.Invoke();
    }
}
