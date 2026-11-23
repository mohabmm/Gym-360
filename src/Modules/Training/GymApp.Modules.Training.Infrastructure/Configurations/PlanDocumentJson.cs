using System.Text.Json;
using System.Text.Json.Serialization;
using GymApp.Modules.Training.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

/// <summary>Stores a <see cref="PlanDocument"/> (plan schema v1) or a workout's sets as camelCase JSON in an nvarchar(max) column.</summary>
internal static class PlanDocumentJson
{
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static PlanDocument Deserialize(string json) =>
        JsonSerializer.Deserialize<PlanDocument>(json, Options) ?? PlanDocument.Of(Array.Empty<PlanWeek>());

    public static void MapDocument<TEntity>(this EntityTypeBuilder<TEntity> builder, string propertyName)
        where TEntity : class =>
        builder.MapJson(propertyName, "document", Deserialize);

    /// <summary>The sets of a finished workout, as a JSON array.</summary>
    public static void MapSets<TEntity>(this EntityTypeBuilder<TEntity> builder, string propertyName)
        where TEntity : class =>
        builder.MapJson<TEntity, IReadOnlyList<CompletedSet>>(propertyName, "sets",
            json => JsonSerializer.Deserialize<List<CompletedSet>>(json, Options) ?? new List<CompletedSet>());

    private static void MapJson<TEntity, TValue>(
        this EntityTypeBuilder<TEntity> builder, string propertyName, string column, Func<string, TValue> deserialize)
        where TEntity : class
    {
        builder.Property<TValue>(propertyName)
            .HasConversion(v => Serialize(v), s => deserialize(s))
            .HasColumnName(column)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            .Metadata.SetValueComparer(new ValueComparer<TValue>(
                (a, b) => Serialize(a) == Serialize(b),
                v => Serialize(v).GetHashCode(),
                v => deserialize(Serialize(v))));
    }
}
