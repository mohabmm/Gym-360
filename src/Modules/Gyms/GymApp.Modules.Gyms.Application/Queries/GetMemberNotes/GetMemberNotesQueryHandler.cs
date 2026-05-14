using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Queries.GetMemberNotes;

internal sealed class GetMemberNotesQueryHandler : IQueryHandler<GetMemberNotesQuery, List<MemberNoteDto>>
{
    private readonly IMemberNoteRepository _memberNoteRepository;

    public GetMemberNotesQueryHandler(IMemberNoteRepository memberNoteRepository)
    {
        _memberNoteRepository = memberNoteRepository;
    }

    public async Task<Result<List<MemberNoteDto>>> Handle(GetMemberNotesQuery request, CancellationToken cancellationToken)
    {
        var notes = await _memberNoteRepository.GetByMemberAsync(
            request.GymId, request.MemberId, cancellationToken);

        var dtos = notes.Select(n => new MemberNoteDto(
            n.Id,
            n.Content,
            n.CreatedBy,
            n.CreatedAt)).ToList();

        return Result.Success(dtos);
    }
}
