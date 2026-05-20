using System.Net.Http.Json;
using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

public partial class ApiClient
{
    public async Task<List<WorkspaceResponse>> GetWorkspacesAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/workspaces", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<WorkspaceResponse>>(cancellationToken: ct) ?? [];
    }

    public async Task<List<WorkspaceMemberResponse>> GetWorkspaceMembersAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/workspaces/members", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<WorkspaceMemberResponse>>(cancellationToken: ct) ?? [];
    }

    public async Task<WorkspaceResponse?> CreateHouseholdAsync(string name, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/workspaces/household", new CreateHouseholdRequest(name), ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<WorkspaceResponse>(cancellationToken: ct);
    }

    public async Task LeaveHouseholdAsync(CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync("api/workspaces/household", ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<WorkspaceResponse?> RenameWorkspaceAsync(Guid workspaceId, string name, CancellationToken ct = default)
    {
        var response = await _http.PatchAsJsonAsync($"api/workspaces/{workspaceId}", new RenameWorkspaceRequest(name), ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<WorkspaceResponse>(cancellationToken: ct);
    }
}
