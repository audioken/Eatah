using System.Net.Http.Json;
using System.Text.Json;
using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

public class ApiClient
{
    private static readonly JsonSerializerOptions ErrorJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<MealResponse>> GetMealsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("api/meals", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<MealResponse>>(cancellationToken: cancellationToken)
            ?? [];
    }

    public async Task<MealResponse?> GetMealAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"api/meals/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<MealResponse>(cancellationToken: cancellationToken);
    }

    public async Task<MealResponse?> CreateMealAsync(CreateMealRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/meals", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<MealResponse>(cancellationToken: cancellationToken);
    }

    public async Task<MealResponse?> UpdateMealAsync(Guid id, UpdateMealRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"api/meals/{id}", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<MealResponse>(cancellationToken: cancellationToken);
    }

    public async Task DeleteMealAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"api/meals/{id}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<WeeklyPlanResponse?> GetCurrentWeeklyPlanAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("api/weeklyplans/current", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>(cancellationToken: cancellationToken);
    }

    public async Task<WeeklyPlanResponse?> AssignMealAsync(
        Guid planId,
        DayOfWeek day,
        Guid mealId,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync(
            $"api/weeklyplans/{planId}/days/{day}",
            new AssignMealRequest(mealId),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>(cancellationToken: cancellationToken);
    }

    public async Task<WeeklyPlanResponse?> ClearDayAsync(
        Guid planId,
        DayOfWeek day,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"api/weeklyplans/{planId}/days/{day}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>(cancellationToken: cancellationToken);
    }

    public async Task<WeeklyPlanResponse?> RandomizeWeekAsync(
        Guid planId,
        Guid? profileId,
        double strictness,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/weeklyplans/{planId}/randomize",
            new RandomizeWeeklyPlanRequest(profileId, strictness),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>(cancellationToken: cancellationToken);
    }

    public async Task<WeeklyPlanResponse?> RandomizeDayAsync(
        Guid planId,
        DayOfWeek day,
        Guid? profileId,
        double strictness,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/weeklyplans/{planId}/days/{day}/randomize",
            new RandomizeDayRequest(profileId, strictness),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>(cancellationToken: cancellationToken);
    }

    public async Task<AiGeneratedMealResponse?> GenerateAiMealAsync(
        GenerateMealRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/ai/meals/generate", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<AiGeneratedMealResponse>(cancellationToken: cancellationToken);
    }

    public async Task<List<DietProfileResponse>> GetDietProfilesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("api/dietprofiles", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<DietProfileResponse>>(cancellationToken: cancellationToken)
            ?? [];
    }

    public async Task<DietEvaluationResponse?> EvaluateWeeklyPlanAsync(
        Guid planId,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync(
            $"api/weeklyplans/{planId}/evaluate?profileId={profileId}",
            content: null,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<DietEvaluationResponse>(cancellationToken: cancellationToken);
    }

    public async Task<DietProfileResponse?> GenerateDietProfileAsync(
        GenerateDietProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/dietprofiles/generate", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<DietProfileResponse>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Throws an <see cref="ApiException"/> carrying the parsed <see cref="ApiErrorResponse"/>
    /// when the response is non-success. Falls back to a generic error if the body is not ProblemDetails.
    /// </summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ApiErrorResponse? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(ErrorJsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            // Body wasn't ProblemDetails JSON – fall through.
        }
        catch (NotSupportedException)
        {
            // Non-JSON content type – fall through.
        }

        error ??= new ApiErrorResponse(
            Status: (int)response.StatusCode,
            Title: response.ReasonPhrase,
            Detail: null,
            ErrorCode: ApiErrorCodes.Unexpected,
            Errors: null);

        if (error.Status == 0)
        {
            error = error with { Status = (int)response.StatusCode };
        }

        throw new ApiException(error);
    }
}
