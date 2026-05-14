using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;

namespace GymApp.Modules.Gyms.Application.Commands.AddMemberNote;

internal sealed class AddMemberNoteCommandHandler : ICommandHandler<AddMemberNoteCommand, Guid>
{
    private readonly IMemberNoteRepository _memberNoteRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public AddMemberNoteCommandHandler(
        IMemberNoteRepository memberNoteRepository,
        IGymsUnitOfWork unitOfWork)
    {
        _memberNoteRepository = memberNoteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddMemberNoteCommand request, CancellationToken cancellationToken)
    {
        var note = MemberNote.Create(
            request.GymId,
            request.MemberId,
            request.CreatedBy,
            request.Content);

        await _memberNoteRepository.AddAsync(note, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(note.Id);
    }
}
