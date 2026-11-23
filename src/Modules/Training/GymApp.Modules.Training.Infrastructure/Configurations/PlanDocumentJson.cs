using System.Text.Json;
using System.Text.Json.Serialization;
using GymApp.Modules.Training.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApp.Modules.Training.Infrastructure.Configurations;

/// <summary>Stores a <see cref="PlanDocument"/> as camelCase JSON (plan schema v1) in an nvarchar(max) column.</summary>
internal static class PlanDocumentJson
{
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(PlanDocument document) => JsonSerializer.Serialize(document, Options);

    public static PlanDocument Deserialize(string json) =>
        JsonSerializer.Deserialize<PlanDocument>(json, Options) ?? PlanDocument.Of(Array.Empty<PlanWeek>());

    public static void MapDocument<TEntity>(this EntityTypeBuilder<TEntity> builder, string propertyName)
        where TEntity : class
    {
        builder.Property<PlanDocument>(propertyName)
            .HasConversion(d => Serialize(d), s => Deserialize(s))
            .HasColumnName("document")
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            .Metadata.SetValueComparer(new ValueComparer<PlanDocument>(
                (a, b) => Serialize(a!) == Serialize(b!),
                d => Serialize(d).GetHashCode(),
                d => Deserialize(Serialize(d))));
    }
}
