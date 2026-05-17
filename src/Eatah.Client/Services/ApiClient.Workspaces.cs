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
}
