using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Application.Abstractions;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateGym;

internal sealed class UpdateGymCommandHandler : ICommandHandler<UpdateGymCommand>
{
    private readonly IGymRepository _gymRepository;
    private readonly IGymsUnitOfWork _unitOfWork;

    public UpdateGymCommandHandler(IGymRepository gymRepository, IGymsUnitOfWork unitOfWork)
    {
        _gymRepository = gymRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateGymCommand request, CancellationToken cancellationToken)
    {
        var gym = await _gymRepository.GetByIdAsync(request.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymErrors.NotFound);

        var name = request.Name ?? gym.Name;
        var description = request.Description ?? gym.Description;

        var address = request.Address is not null
            ? new Address(
                request.Address.Street,
                request.Address.City,
                request.Address.State,
                request.Address.Country,
                request.Address.ZipCode,
                request.Address.Latitude,
                request.Address.Longitude)
            : gym.Address;

        var contact = request.Contact is not null
            ? new ContactInfo(
                request.Contact.Email,
                request.Contact.Phone,
                request.Contact.Website)
            : gym.ContactInfo;

        gym.UpdateDetails(name, description, address, contact);

        if (request.Amenities is not null)
            gym.SetAmenities(request.Amenities);

        _gymRepository.Update(gym);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
