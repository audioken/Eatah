using System.Net.Http.Json;
using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<MealResponse>> GetMealsAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<List<MealResponse>>("api/meals", cancellationToken)
            ?? [];
    }

    public async Task<MealResponse?> GetMealAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<MealResponse>($"api/meals/{id}", cancellationToken);
    }

    public async Task<MealResponse?> CreateMealAsync(CreateMealRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/meals", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MealResponse>(cancellationToken: cancellationToken);
    }

    public async Task<MealResponse?> UpdateMealAsync(Guid id, UpdateMealRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"api/meals/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MealResponse>(cancellationToken: cancellationToken);
    }

    public async Task DeleteMealAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"api/meals/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<WeeklyPlanResponse?> GetCurrentWeeklyPlanAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<WeeklyPlanResponse>("api/weeklyplans/current", cancellationToken);
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
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>(cancellationToken: cancellationToken);
    }

    public async Task<WeeklyPlanResponse?> ClearDayAsync(
        Guid planId,
        DayOfWeek day,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"api/weeklyplans/{planId}/days/{day}", cancellationToken);
        response.EnsureSuccessStatusCode();
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
        response.EnsureSuccessStatusCode();
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
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WeeklyPlanResponse>(cancellationToken: cancellationToken);
    }

    public async Task<AiGeneratedMealResponse?> GenerateAiMealAsync(
        GenerateMealRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/ai/meals/generate", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"AI-generering misslyckades ({(int)response.StatusCode}): {detail}");
        }
        return await response.Content.ReadFromJsonAsync<AiGeneratedMealResponse>(cancellationToken: cancellationToken);
    }

    public async Task<List<DietProfileResponse>> GetDietProfilesAsync(CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<List<DietProfileResponse>>("api/dietprofiles", cancellationToken)
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
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DietEvaluationResponse>(cancellationToken: cancellationToken);
    }

    public async Task<DietProfileResponse?> GenerateDietProfileAsync(
        GenerateDietProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/dietprofiles/generate", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"AI-generering misslyckades ({(int)response.StatusCode}): {detail}");
        }
        return await response.Content.ReadFromJsonAsync<DietProfileResponse>(cancellationToken: cancellationToken);
    }
}
