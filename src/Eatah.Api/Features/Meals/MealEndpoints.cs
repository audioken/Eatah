using FluentValidation;

namespace Eatah.Api.Features.Meals;

public static class MealEndpoints
{
    public static IEndpointRouteBuilder MapMealEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/meals")
            .WithTags("Meals");

        group.MapGet("/", GetAllMeals.Handle).WithName(nameof(GetAllMeals));
        group.MapGet("/{id:guid}", GetMealById.Handle).WithName(nameof(GetMealById));
        group.MapPost("/", CreateMeal.Handle).WithName(nameof(CreateMeal));
        group.MapPut("/{id:guid}", UpdateMeal.Handle).WithName(nameof(UpdateMeal));
        group.MapDelete("/{id:guid}", DeleteMeal.Handle).WithName(nameof(DeleteMeal));

        return app;
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
