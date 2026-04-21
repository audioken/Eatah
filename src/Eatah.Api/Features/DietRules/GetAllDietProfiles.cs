namespace Eatah.Api.Features.DietRules;

public static class GetAllDietProfiles
{
    public static async Task<IResult> Handle(DietRuleService service, CancellationToken cancellationToken)
    {
        var profiles = await service.GetAllProfilesAsync(cancellationToken);
        return Results.Ok(profiles);
    }
}
