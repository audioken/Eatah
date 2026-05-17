using Eatah.Api.Common;

namespace Eatah.Api.Features.Workspaces;

public static class WorkspaceEndpoints
{
    public static void MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workspaces")
            .WithTags("Workspaces")
            .RequireAuthorization();

        group.MapGet("/", GetMyWorkspaces.Handle);
        group.MapGet("/members", GetWorkspaceMembers.Handle);
        group.MapPost("/household", CreateHousehold.Handle);
        group.MapDelete("/household", LeaveHousehold.Handle);
        group.MapPatch("/{id:guid}", RenameWorkspace.Handle);
    }

    public static IServiceCollection AddWorkspaceFeature(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IWorkspaceContext, WorkspaceContext>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<WorkspaceService>();
        return services;
    }
}
