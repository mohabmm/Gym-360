using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Queries.GetAnnouncements;

public sealed record GetAnnouncementsQuery(
    Guid GymId,
    int Page,
    int PageSize) : IQuery<PagedResponse<AnnouncementDto>>;
