using FluentAssertions;
using GymApp.Common.Application.FeatureFlags;
using GymApp.Common.Presentation.FeatureFlags;
using Xunit;

namespace GymApp.Tests.Architecture;

public class FeatureRouteMapTests
{
    [Theory]
    [InlineData("GET", "api/v1/gyms/{gymId:guid}/classes", FeatureKeys.Classes)]
    [InlineData("POST", "api/v1/gyms/{gymId:guid}/classes/{classId:guid}/book", FeatureKeys.Classes)]
    [InlineData("POST", "api/v1/gyms/{gymId:guid}/check-in", FeatureKeys.GymAttendance)]
    [InlineData("GET", "api/v1/gyms/{gymId:guid}/check-ins", FeatureKeys.GymAttendance)]
    [InlineData("POST", "api/v1/gyms/{gymId:guid}/members/{memberId:guid}/freeze", FeatureKeys.GymMemberships)]
    [InlineData("GET", "api/v1/gyms/{gymId:guid}/members/{memberId:guid}/notes", FeatureKeys.GymMemberRecords)]
    [InlineData("PUT", "api/v1/gyms/{gymId:guid}/trainers/{trainerId:guid}/rate", FeatureKeys.GymFinance)]
    [InlineData("POST", "api/v1/billing/payments", FeatureKeys.GymFinance)]
    [InlineData("POST", "api/v1/billing/packages/quick-assign", FeatureKeys.PtPackages)]
    [InlineData("GET", "api/v1/billing/me/packages", FeatureKeys.PtPackages)]
    [InlineData("GET", "api/v1/scheduling/sessions", FeatureKeys.PtDualSessionLogging)]
    [InlineData("POST", "api/v1/scheduling/sessions", FeatureKeys.SchedulingBooking)]
    [InlineData("GET", "api/v1/scheduling/sessions/{sessionId:guid}", FeatureKeys.PtDualSessionLogging)]
    [InlineData("POST", "api/v1/scheduling/sessions/{sessionId:guid}/trainer-log", FeatureKeys.PtDualSessionLogging)]
    [InlineData("POST", "api/v1/scheduling/sessions/{sessionId:guid}/start", FeatureKeys.SchedulingBooking)]
    [InlineData("POST", "api/v1/scheduling/sessions/{sessionId:guid}/exercise-logs", FeatureKeys.TrainingWorkouts)]
    [InlineData("POST", "api/v1/progress/body-metrics/client", FeatureKeys.PtProgressMeasurements)]
    [InlineData("POST", "api/v1/progress/photos", FeatureKeys.ProgressPhotos)]
    [InlineData("GET", "api/v1/training/programs/{programId:guid}", FeatureKeys.TrainingPrograms)]
    [InlineData("GET", "api/v1/training/workouts", FeatureKeys.TrainingWorkouts)]
    [InlineData("POST", "api/v1/training/remote-workout/activate", FeatureKeys.TrainingRemoteWorkout)]
    [InlineData("GET", "api/v1/nutrition/food-log", FeatureKeys.Nutrition)]
    [InlineData("GET", "api/v1/communication/conversations", FeatureKeys.CommunicationChat)]
    [InlineData("POST", "api/v1/communication/broadcast", FeatureKeys.CommunicationAnnouncements)]
    [InlineData("GET", "api/v1/gamification/trainers/leaderboard", FeatureKeys.CoachLeaderboard)]
    [InlineData("GET", "api/v1/gamification/trainers/{trainerId:guid}/ratings", FeatureKeys.PtCoachRatings)]
    [InlineData("PUT", "api/v1/gyms/{gymId:guid}/branding", FeatureKeys.GymBranding)]
    [InlineData("GET", "api/v1/owners/{ownerId:guid}/dashboard", FeatureKeys.Analytics)]
    public void Maps_route_to_its_module_flag(string method, string template, string expectedKey)
    {
        FeatureRouteMap.Resolve(method, template)!.FeatureKey.Should().Be(expectedKey);
    }

    [Theory]
    [InlineData("POST", "api/v1/identity/login")]
    [InlineData("GET", "api/v1/identity/me")]
    [InlineData("GET", "api/v1/gyms/{gymId:guid}")]
    [InlineData("GET", "api/v1/gyms/{gymId:guid}/members")]
    [InlineData("POST", "api/v1/gyms/{gymId:guid}/members")]
    [InlineData("GET", "api/v1/gyms/{gymId:guid}/trainers/{trainerId:guid}/clients")]
    [InlineData("GET", "api/v1/gyms/{gymId:guid}/branding")]
    [InlineData("GET", "api/v1/communication/notifications")]
    [InlineData("POST", "api/v1/notifications/device-tokens")]
    [InlineData("GET", "api/v1/feature-flags/me")]
    [InlineData("GET", "api/v1/admin/feature-flags/gyms")]
    [InlineData("GET", "api/v1/gamification/trainers/{trainerId:guid}/public-profile")]
    [InlineData("GET", "/health/live")]
    public void Core_routes_are_always_on(string method, string template)
    {
        FeatureRouteMap.Resolve(method, template)!.IsAlwaysOn.Should().BeTrue();
    }

    [Fact]
    public void Unknown_route_is_unmapped()
    {
        FeatureRouteMap.Resolve("GET", "api/v1/something-new").Should().BeNull();
    }
}
