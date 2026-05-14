namespace GymApp.Modules.Billing.Infrastructure.Persistence.ReadModels;

/// <summary>
/// Lightweight read model for gyms.trainer_rates.
/// Allows the Billing module to look up the trainer's configured rate per session
/// when recording earnings, without referencing Gyms.Domain or Gyms.Infrastructure.
/// </summary>
internal sealed class TrainerRateReadModel
{
    public Guid Id { get; set; }
    public Guid GymId { get; set; }
    public Guid TrainerId { get; set; }
    public decimal RatePerSession { get; set; }
}
