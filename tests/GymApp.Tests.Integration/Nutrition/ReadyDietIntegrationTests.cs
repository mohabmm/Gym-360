using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Tests.Integration.Abstractions;
using GymApp.Tests.Integration.Pt;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Integration.Nutrition;

/// <summary>
/// Ready diets as part of onboarding: the coach calculates a new member's calories, gives a
/// ready Egyptian diet scaled to them, the member is notified and ticks today's meals (once),
/// and the coach sees the week. Only the member's coach can give it; the nutrition flag closes it.
/// </summary>
public sealed class ReadyDietIntegrationTests : BaseIntegrationTest
{
    private const string Nutrition = "api/v1/nutrition";
    private const string Today = "api/v1/nutrition/me/meal-plan/today";
    private static readonly TimeSpan OutboxTimeout = TimeSpan.FromSeconds(60);

    private readonly Guid _coachId = Guid.NewGuid();
    private readonly Guid _traineeId = Guid.NewGuid();
    private readonly Guid _deskId = Guid.NewGuid();
    private Guid _gymId;

    public ReadyDietIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Coach_calculates_gives_a_scaled_diet_and_the_member_ticks_todays_meals_once()
    {
        var (coach, trainee) = await StartAsync();

        var target = await PostDataAsync(coach, $"{Nutrition}/calorie-target",
            new { sex = "Male", age = 30, heightCm = 180, weightKg = 80, activity = "Moderate", goal = "FatLoss" });
        target.GetProperty("calories").GetInt32().Should().Be(2210);
        target.GetProperty("proteinGrams").GetInt32().Should().Be(160);

        var diets = (await PtTestGym.DataAsync(coach, $"{Nutrition}/ready-diets?goal=FatLoss")).EnumerateArray().ToList();
        diets.Should().HaveCount(5);
        diets[0].GetProperty("key").GetString().Should().Be("fat_loss_high_protein", "the goal's diet comes first");
        diets[0].GetProperty("nameAr").GetString().Should().StartWith("تخسيس");

        var preview = await PtTestGym.DataAsync(coach, $"{Nutrition}/ready-diets/fat_loss_high_protein?calories=2210");
        preview.GetProperty("days").GetArrayLength().Should().Be(7);

        var plan = await PostDataAsync(coach, $"{Nutrition}/clients/{_traineeId}/diet", new
        {
            dietKey = "fat_loss_high_protein",
            target = new { calories = 2210, proteinGrams = 160, carbsGrams = 260, fatGrams = 60 },
        });
        plan.GetProperty("clientId").GetGuid().Should().Be(_traineeId);
        plan.GetProperty("days").GetArrayLength().Should().Be(7);
        plan.GetProperty("days").EnumerateArray().Should().OnlyContain(d => d.GetProperty("plannedMeals").GetArrayLength() == 5,
            "every day keeps its meals, also the days that repeat day A");
        plan.GetProperty("dailyCalorieTarget").GetDecimal().Should().Be(2210);

        (await PtTestGym.DataAsync(trainee, $"{Nutrition}/me/meal-plan")).GetProperty("name").GetString()
            .Should().Be("تخسيس بأكل مصري – بروتين عالي", "the member sees the plan right away");

        var today = await PtTestGym.DataAsync(trainee, Today);
        var meals = today.GetProperty("meals").EnumerateArray().ToList();
        meals.Should().HaveCount(5);
        meals.Should().OnlyContain(m => !m.GetProperty("done").GetBoolean());
        meals[0].GetProperty("alternative").GetProperty("name").GetString().Should().StartWith("بديل");

        await PostDataAsync(trainee, $"{Today}/0/done", new { alternative = false });
        var ticked = await PostDataAsync(trainee, $"{Today}/0/done", new { alternative = false });
        ticked.GetProperty("meals")[0].GetProperty("done").GetBoolean().Should().BeTrue();
        ticked.GetProperty("eatenCalories").GetDecimal()
            .Should().Be(ticked.GetProperty("meals")[0].GetProperty("meal").GetProperty("totalCalories").GetDecimal(), "a second tap logs nothing more");

        var week = (await PtTestGym.DataAsync(coach, $"{Nutrition}/clients/{_traineeId}/diet")).GetProperty("lastWeek").EnumerateArray().ToList();
        week.Should().HaveCount(7);
        week.Last().GetProperty("mealsDone").GetInt32().Should().Be(1);
        week.Last().GetProperty("mealsPlanned").GetInt32().Should().Be(5);

        await EventuallyAsync(() => HasNotificationAsync(trainee, "meal_plan_assigned"), "the member hears about the diet");
    }

    [Fact]
    public async Task Ticking_the_alternative_counts_the_meal_as_eaten()
    {
        var (coach, trainee) = await StartAsync();
        await GiveAsync(coach);

        var ticked = await PostDataAsync(trainee, $"{Today}/2/done", new { alternative = true });

        ticked.GetProperty("meals")[2].GetProperty("done").GetBoolean().Should().BeTrue();
        ticked.GetProperty("eatenCalories").GetDecimal()
            .Should().Be(ticked.GetProperty("meals")[2].GetProperty("alternative").GetProperty("totalCalories").GetDecimal());
    }

    [Fact]
    public async Task Only_the_members_coach_can_give_or_see_their_diet()
    {
        var (_, trainee) = await StartAsync();
        var stranger = CreateTrainerClient(Guid.NewGuid(), _gymId);

        var give = await stranger.PostAsJsonAsync($"{Nutrition}/clients/{_traineeId}/diet", GiveBody());
        give.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await stranger.GetAsync($"{Nutrition}/clients/{_traineeId}/diet")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await trainee.PostAsJsonAsync($"{Nutrition}/clients/{_traineeId}/diet", GiveBody())).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await PtTestGym.DataAsync(trainee, Today)).ValueKind.Should().Be(JsonValueKind.Null, "no diet yet");
    }

    [Fact]
    public async Task Bad_input_is_refused_with_a_reason()
    {
        var (coach, _) = await StartAsync();

        var tooYoung = await coach.PostAsJsonAsync($"{Nutrition}/calorie-target",
            new { sex = "Female", age = 12, heightCm = 150, weightKg = 40, activity = "Light", goal = "Maintain" });
        tooYoung.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await tooYoung.Content.ReadAsStringAsync()).Should().Contain("Nutrition.Diet.InvalidBody");
        (await coach.GetAsync($"{Nutrition}/ready-diets/nope?calories=2000")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await coach.GetAsync($"{Nutrition}/ready-diets/ramadan?calories=300")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task With_nutrition_off_every_diet_endpoint_is_closed()
    {
        var (coach, trainee) = await StartAsync();
        await SetFlagAsync(FeatureKeys.Nutrition, enabled: false);

        foreach (var url in new[] { $"{Nutrition}/ready-diets", $"{Nutrition}/clients/{_traineeId}/diet" })
            (await coach.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.Forbidden, url);
        (await trainee.GetAsync(Today)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------ helpers

    private static object GiveBody() => new
    {
        dietKey = "balanced_egyptian",
        target = new { calories = 2000, proteinGrams = 120, carbsGrams = 240, fatGrams = 55 },
    };

    private async Task GiveAsync(HttpClient coach) => await PostDataAsync(coach, $"{Nutrition}/clients/{_traineeId}/diet", GiveBody());

    /// <summary>A business gym (nutrition on) where the trainee has a package with the coach.</summary>
    private async Task<(HttpClient Coach, HttpClient Trainee)> StartAsync()
    {
        _gymId = await PtTestGym.RegisterAsync(Factory.Services, _traineeId, FeatureRegistry.BusinessPreset);
        var desk = CreateStaffClient(_deskId, _gymId);
        var assigned = await desk.PostAsJsonAsync("api/v1/billing/packages/quick-assign", new
        {
            gymId = _gymId, trainerId = _coachId, memberId = _traineeId, sessionCount = 4, assignedBy = _deskId, isTrial = true,
        });
        assigned.StatusCode.Should().Be(HttpStatusCode.Created, await assigned.Content.ReadAsStringAsync());
        return (WithGym(CreateTrainerClient(_coachId, _gymId)), WithGym(CreateTraineeClient(_traineeId, _gymId)));
    }

    private HttpClient WithGym(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-Gym-Id", _gymId.ToString());
        return client;
    }

    private static async Task<JsonElement> PostDataAsync(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"{url} → {text}");
        return JsonDocument.Parse(text).RootElement.GetProperty("data").Clone();
    }

    private async Task SetFlagAsync(string key, bool enabled)
    {
        using var scope = Factory.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IFeatureFlagAdminService>();
        (await admin.SetOverrideAsync(_gymId, new FlagOverrideChange(key, enabled, "test"), new AdminActor(Guid.NewGuid(), "test")))
            .IsSuccess.Should().BeTrue();
    }

    private static async Task<bool> HasNotificationAsync(HttpClient client, string type)
    {
        var data = await PtTestGym.DataAsync(client, "api/v1/communication/notifications?page=1&pageSize=100");
        var items = data.ValueKind == JsonValueKind.Array ? data : data.GetProperty("items");
        return items.EnumerateArray().Any(n =>
            n.GetProperty("data").TryGetProperty("type", out var t) && t.GetString() == type);
    }

    private static async Task EventuallyAsync(Func<Task<bool>> condition, string because)
    {
        var deadline = DateTime.UtcNow + OutboxTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
        (await condition()).Should().BeTrue(because);
    }
}
