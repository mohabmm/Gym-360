using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Events;

namespace GymApp.Modules.Training.Domain.Entities;

public sealed class WorkoutAssignment : Entity
{
    private WorkoutAssignment() { }
    private WorkoutAssignment(Guid id) : base(id) { }

    public Guid TrainerId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid GymId { get; private set; }
    public Guid? WorkoutId { get; private set; }
    public Guid? ProgramId { get; private set; }
    public AssignmentType AssignmentType { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public AssignmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public bool IsRemoteMode { get; private set; }

    public static WorkoutAssignment Create(
        Guid trainerId,
        Guid clientId,
        Guid gymId,
        AssignmentType assignmentType,
        DateOnly startDate,
        Guid? workoutId = null,
        Guid? programId = null,
        DateOnly? endDate = null,
        string? notes = null,
        bool isRemoteMode = false)
    {
        var assignment = new WorkoutAssignment(Guid.NewGuid())
        {
            TrainerId = trainerId,
            ClientId = clientId,
            GymId = gymId,
            WorkoutId = workoutId,
            ProgramId = programId,
            AssignmentType = assignmentType,
            StartDate = startDate,
            EndDate = endDate,
            Status = AssignmentStatus.Active,
            Notes = notes,
            IsRemoteMode = isRemoteMode
        };

        var domainEvent = assignmentType == AssignmentType.Program
            ? (IDomainEvent)new ProgramAssignedDomainEvent(assignment.Id, trainerId, clientId)
            : new WorkoutAssignedDomainEvent(assignment.Id, trainerId, clientId);

        assignment.RaiseDomainEvent(domainEvent);

        return assignment;
    }

    public void Complete()
    {
        Status = AssignmentStatus.Completed;
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public void Cancel()
    {
        Status = AssignmentStatus.Cancelled;
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
