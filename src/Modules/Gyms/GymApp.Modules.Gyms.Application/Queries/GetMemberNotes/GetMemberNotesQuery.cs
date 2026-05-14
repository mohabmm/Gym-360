using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Queries.GetMemberNotes;

public sealed record MemberNoteDto(
    Guid Id,
    string Content,
    Guid CreatedBy,
    DateTime CreatedAt);

public sealed record GetMemberNotesQuery(
    Guid GymId,
    Guid MemberId) : IQuery<List<MemberNoteDto>>;
