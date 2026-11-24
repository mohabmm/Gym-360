using GymApp.Common.Domain;

namespace GymApp.Modules.Nutrition.Domain.Diet;

public static class DietErrors
{
    public static Error InvalidBody(string reason) => new("Nutrition.Diet.InvalidBody", reason);

    public static readonly Error NotYourClient = new("Nutrition.Diet.Forbidden", "This member is not your client.");

    public static readonly Error DietNotFound = new("Nutrition.Diet.NotFound", "There is no ready diet with this key.");

    public static readonly Error InvalidTarget = new("Nutrition.Diet.InvalidTarget", "The daily calories must be 1000 to 5000.");

    public static readonly Error NoPlan = new("Nutrition.Diet.PlanNotFound", "You have no diet plan yet.");

    public static readonly Error MealNotFound = new("Nutrition.Diet.MealNotFound", "Today's plan has no meal at this position.");
}
