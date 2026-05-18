using Eatah.Api.Common;

namespace Eatah.Api.Features.DietRules;

public static class DeleteDietProfile
{
    public static async Task<IResult> Handle(
        Guid id,
        DietRuleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        return result.ToNoContentResult();
    }
}
