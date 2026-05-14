namespace GymApp.Modules.Progress.Application.DTOs;

public sealed record BodyMeasurementsDto(
    decimal? Chest,
    decimal? Waist,
    decimal? Hips,
    decimal? LeftArm,
    decimal? RightArm,
    decimal? LeftThigh,
    decimal? RightThigh,
    decimal? LeftCalf,
    decimal? RightCalf,
    decimal? Neck,
    string MeasurementUnit);
