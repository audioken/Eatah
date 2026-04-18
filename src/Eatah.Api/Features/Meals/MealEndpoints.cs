using FluentValidation;

namespace Eatah.Api.Features.Meals;

public static class MealEndpoints
{
    public static IEndpointRouteBuilder MapMealEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/meals")
            .WithTags("Meals");

        group.MapGet("/", GetAllMeals)
            .WithName("GetAllMeals");

        group.MapGet("/{id:guid}", GetMealById)
            .WithName("GetMealById");

        group.MapPost("/", CreateMeal)
            .WithName("CreateMeal");

        group.MapPut("/{id:guid}", UpdateMeal)
            .WithName("UpdateMeal");

        group.MapDelete("/{id:guid}", DeleteMeal)
            .WithName("DeleteMeal");

        return app;
    }

    private static async Task<IResult> GetAllMeals(MealService service, CancellationToken cancellationToken)
    {
        var meals = await service.GetAllAsync(cancellationToken);
        return Results.Ok(meals);
    }

    private static async Task<IResult> GetMealById(Guid id, MealService service, CancellationToken cancellationToken)
    {
        var meal = await service.GetByIdAsync(id, cancellationToken);
        return meal is null
            ? Results.NotFound(new { detail = $"Maträtt med id {id} hittades inte." })
            : Results.Ok(meal);
    }

    private static async Task<IResult> CreateMeal(
        CreateMealRequest request,
        MealService service,
        IValidator<CreateMealRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var created = await service.CreateAsync(request, cancellationToken);
        return Results.Created($"/api/meals/{created.Id}", created);
    }

    private static async Task<IResult> UpdateMeal(
        Guid id,
        UpdateMealRequest request,
        MealService service,
        IValidator<UpdateMealRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        try
        {
            var updated = await service.UpdateAsync(id, request, cancellationToken);
            return Results.Ok(updated);
        }
        catch (MealNotFoundException ex)
        {
            return Results.NotFound(new { detail = ex.Message });
        }
    }

    private static async Task<IResult> DeleteMeal(Guid id, MealService service, CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(id, cancellationToken);
        return deleted
            ? Results.NoContent()
            : Results.NotFound(new { detail = $"Maträtt med id {id} hittades inte." });
    }
}

public static class MealServiceExtensions
{
    public static IServiceCollection AddMealFeature(this IServiceCollection services)
    {
        services.AddScoped<IMealRepository, MealRepository>();
        services.AddScoped<MealService>();
        services.AddScoped<IValidator<CreateMealRequest>, CreateMealRequestValidator>();
        services.AddScoped<IValidator<UpdateMealRequest>, UpdateMealRequestValidator>();
        return services;
    }
}
