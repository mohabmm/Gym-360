using GymApp.Common.Application.Messaging;
using GymApp.Modules.Identity.Application.DTOs;

namespace GymApp.Modules.Identity.Application.Queries.LookupTraineeByPhone;

/// <summary>
/// Resolves an existing trainee by (normalized) phone number so staff can confirm a
/// returning member's identity before re-linking them to their gym. Fails with
/// User.NotFound when no trainee matches.
/// </summary>
public sealed record LookupTraineeByPhoneQuery(string Phone) : IQuery<TraineeLookupResponse>;
