using GymApp.Common.Domain;

namespace GymApp.Modules.Gamification.Domain.Errors;

public static class GamificationErrors
{
    public static readonly Error LeaderboardNotFound = new(
        "Gamification.LeaderboardNotFound",
        "The leaderboard was not found.");

    public static readonly Error RatingNotFound = new(
        "Gamification.RatingNotFound",
        "The trainer rating was not found.");

    public static readonly Error ProfileNotFound = new(
        "Gamification.ProfileNotFound",
        "The trainer public profile was not found.");

    public static readonly Error TestimonialNotFound = new(
        "Gamification.TestimonialNotFound",
        "The testimonial was not found.");

    public static readonly Error AlreadyRatedForSession = new(
        "Gamification.AlreadyRatedForSession",
        "A rating has already been submitted for this session.");

    public static readonly Error InvalidStarRating = new(
        "Gamification.InvalidStarRating",
        "Star rating must be between 1 and 5.");

    public static readonly Error OptOutNotFound = new(
        "Gamification.OptOutNotFound",
        "The leaderboard opt-out record was not found.");

    public static readonly Error InsufficientRatingsForCoachOfMonth = new(
        "Gamification.InsufficientRatingsForCoachOfMonth",
        "A minimum of 3 unique clients is required for Coach of the Month selection.");

    public static readonly Error TrainerScoreNotFound = new(
        "Gamification.TrainerScoreNotFound",
        "The trainer monthly score was not found.");
}
