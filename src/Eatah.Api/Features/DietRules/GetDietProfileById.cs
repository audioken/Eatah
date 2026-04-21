using Eatah.Api.Common;

namespace Eatah.Api.Features.DietRules;

public static class GetDietProfileById
{
    public static async Task<IResult> Handle(Guid id, DietRuleService service, CancellationToken cancellationToken)
    {
        var result = await service.GetProfileAsync(id, cancellationToken);
        return result.ToHttpResult();
    }
}
