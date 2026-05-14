namespace GymApp.Modules.Gyms.Domain.ValueObjects;

public sealed record Address(
    string Street,
    string City,
    string State,
    string Country,
    string ZipCode,
    double? Latitude = null,
    double? Longitude = null);
