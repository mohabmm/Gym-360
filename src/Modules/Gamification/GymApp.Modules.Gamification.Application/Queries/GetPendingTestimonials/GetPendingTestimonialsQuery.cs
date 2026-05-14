using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gamification.Application.DTOs;

namespace GymApp.Modules.Gamification.Application.Queries.GetPendingTestimonials;

public sealed record GetPendingTestimonialsQuery(Guid GymId)
    : IQuery<List<PendingTestimonialDto>>;
