using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.ValueObjects;

namespace GymApp.Modules.Progress.Application.Mappers;

internal static class ProgressMapper
{
    public static BodyMetricDto ToDto(BodyMetricEntry entry) =>
        new(
            entry.Id,
            entry.UserId,
            entry.GymId,
            entry.Date,
            entry.Weight,
            entry.WeightUnit.ToString(),
            entry.BodyFatPercentage,
            entry.Measurements is not null ? ToDto(entry.Measurements) : null,
            entry.Source.ToString(),
            entry.LoggedBy,
            entry.Notes,
            entry.CreatedAt);

    public static BodyMeasurementsDto ToDto(BodyMeasurements m) =>
        new(
            m.Chest,
            m.Waist,
            m.Hips,
            m.LeftArm,
            m.RightArm,
            m.LeftThigh,
            m.RightThigh,
            m.LeftCalf,
            m.RightCalf,
            m.Neck,
            m.MeasurementUnit.ToString());

    public static BodyMeasurements? ToDomain(BodyMeasurementsDto? dto)
    {
        if (dto is null) return null;

        if (!Enum.TryParse<Domain.Enums.MeasurementUnit>(dto.MeasurementUnit, true, out var unit))
            unit = Domain.Enums.MeasurementUnit.Cm;

        return new BodyMeasurements
        {
            MeasurementUnit = unit,
            Chest = dto.Chest,
            Waist = dto.Waist,
            Hips = dto.Hips,
            LeftArm = dto.LeftArm,
            RightArm = dto.RightArm,
            LeftThigh = dto.LeftThigh,
            RightThigh = dto.RightThigh,
            LeftCalf = dto.LeftCalf,
            RightCalf = dto.RightCalf,
            Neck = dto.Neck
        };
    }

    public static ProgressPhotoDto ToDto(ProgressPhoto photo) =>
        new(
            photo.Id,
            photo.UserId,
            photo.GymId,
            photo.PhotoUrl,
            photo.ThumbnailUrl,
            photo.Angle.ToString(),
            photo.Date,
            photo.IsSharedWithTrainer,
            photo.Notes,
            photo.CreatedAt);
}
