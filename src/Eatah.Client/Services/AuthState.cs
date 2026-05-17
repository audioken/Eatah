using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// In-memory cache of the currently authenticated user. Single source of truth
/// the UI subscribes to via <see cref="OnChange"/>. The actual session is backed
/// by a JWT stored in <see cref="ITokenStore"/> which <see cref="TokenAuthorizationHandler"/>
/// attaches as a Bearer header on every request.
/// </summary>
public class AuthState
{
    private readonly ApiClient _api;
    private readonly ITokenStore _tokenStore;
    private UserResponse? _currentUser;
    private bool _initialized;

    public AuthState(ApiClient api, ITokenStore tokenStore)
    {
        _api = api;
        _tokenStore = tokenStore;
    }

    public UserResponse? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser is not null;
    public bool IsInitialized => _initialized;

    public event Action? OnChange;

    /// <summary>
    /// Loads the token from persistent storage, then calls <c>GET /api/auth/me</c>
    /// to verify it is still valid. Safe to call multiple times; cheap after the first call.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        await _tokenStore.LoadAsync(ct);
        try
        {
            _currentUser = await _api.GetMeAsync(ct);
            if (_currentUser is null)
            {
                // Token invalid/expired — discard it.
                _tokenStore.Clear();
            }
        }
        catch
        {
            _currentUser = null;
            _tokenStore.Clear();
        }
        _initialized = true;
        OnChange?.Invoke();
    }

    public async Task<UserResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var auth = await _api.LoginAsync(request, ct)
            ?? throw new InvalidOperationException("Login response was empty.");
        _tokenStore.Store(auth.Token);
        _currentUser = new UserResponse(auth.Id, auth.Email, auth.DisplayName);
        _initialized = true;
        OnChange?.Invoke();
        return _currentUser;
    }

    public async Task<UserResponse> ConfirmAndSignInAsync(ConfirmEmailRequest request, CancellationToken ct = default)
    {
        var auth = await _api.ConfirmEmailAsync(request, ct)
            ?? throw new InvalidOperationException("Confirmation response was empty.");
        _tokenStore.Store(auth.Token);
        _currentUser = new UserResponse(auth.Id, auth.Email, auth.DisplayName);
        _initialized = true;
        OnChange?.Invoke();
        return _currentUser;
    }

    public async Task<UserResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var auth = await _api.ResetPasswordAsync(request, ct)
            ?? throw new InvalidOperationException("Reset response was empty.");
        _tokenStore.Store(auth.Token);
        _currentUser = new UserResponse(auth.Id, auth.Email, auth.DisplayName);
        _initialized = true;
        OnChange?.Invoke();
        return _currentUser;
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
        _tokenStore.Clear();
        _currentUser = null;
        OnChange?.Invoke();
    }

    /// <summary>
    /// Called by <see cref="ApiClient"/>-aware code paths when a request unexpectedly returns 401,
    /// indicating the token has expired or been revoked.
    /// </summary>
    public void NotifySessionExpired()
    {
        if (_currentUser is null) return;
        _tokenStore.Clear();
        _currentUser = null;
        OnChange?.Invoke();
    }
}

