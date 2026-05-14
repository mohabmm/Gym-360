using GymApp.Common.Application.Messaging;
using GymApp.Modules.Identity.Application.DTOs;

namespace GymApp.Modules.Identity.Application.Queries.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IQuery<UserProfileResponse>;
