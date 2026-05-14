using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Application.Abstractions;

namespace GymApp.Modules.Gyms.Application.Commands.BookClass;

internal sealed class BookClassCommandHandler : ICommandHandler<BookClassCommand>
{
    private readonly IGymClassRepository _gymClassRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public BookClassCommandHandler(IGymClassRepository gymClassRepository, IGymsUnitOfWork unitOfWork)
    {
        _gymClassRepository = gymClassRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(BookClassCommand request, CancellationToken cancellationToken)
    {
        var gymClass = await _gymClassRepository.GetByIdAsync(request.ClassId, cancellationToken);
        if (gymClass is null || gymClass.GymId != request.GymId)
            return Result.Failure(GymErrors.ClassNotFound);

        var bookingResult = gymClass.Book(request.MemberId);
        if (bookingResult.IsFailure)
            return Result.Failure(bookingResult.Error);

        _gymClassRepository.Update(gymClass);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
