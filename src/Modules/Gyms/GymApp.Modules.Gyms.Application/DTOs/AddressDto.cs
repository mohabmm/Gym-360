namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record AddressDto(
    string Street,
    string City,
    string State,
    string Country,
    string ZipCode,
    double? Latitude = null,
    double? Longitude = null);
