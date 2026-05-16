using System.Net;
using System.Net.Http.Json;
using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

public partial class ApiClient
{
    public async Task<RegistrationResponse?> RegisterAsync(RegisterEmailRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<RegistrationResponse>(cancellationToken: ct);
    }

    public async Task<UserResponse?> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/confirm", request, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<UserResponse>(cancellationToken: ct);
    }

    public async Task<UserResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<UserResponse>(cancellationToken: ct);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/auth/logout", content: null, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task RequestPasswordResetAsync(RequestPasswordResetRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/password-reset/request", request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<UserResponse?> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/password-reset", request, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<UserResponse>(cancellationToken: ct);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/password-change", request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    /// <summary>
    /// Returns the current authenticated user, or <c>null</c> if no session is active.
    /// Unlike other endpoints this does NOT throw on 401 — that's the expected "not logged in" path.
    /// </summary>
    public async Task<UserResponse?> GetMeAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/auth/me", ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<UserResponse>(cancellationToken: ct);
    }

    public async Task<DisplayNameAvailabilityResponse?> CheckDisplayNameAsync(string name, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/auth/check-displayname?name={Uri.EscapeDataString(name)}", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<DisplayNameAvailabilityResponse>(cancellationToken: ct);
    }
}
