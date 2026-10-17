using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Events;
using GymApp.Modules.Training.Domain.ValueObjects;

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

    public static WorkoutAssignment Create(WorkoutAssignmentDetails details)
    {
        var assignment = new WorkoutAssignment(Guid.NewGuid())
        {
            TrainerId = details.TrainerId,
            ClientId = details.ClientId,
            GymId = details.GymId,
            WorkoutId = details.WorkoutId,
            ProgramId = details.ProgramId,
            AssignmentType = details.AssignmentType,
            StartDate = details.StartDate,
            EndDate = details.EndDate,
            Status = AssignmentStatus.Active,
            Notes = details.Notes,
            IsRemoteMode = details.IsRemoteMode
        };

        var domainEvent = details.AssignmentType == AssignmentType.Program
            ? (IDomainEvent)new ProgramAssignedDomainEvent(assignment.Id, details.TrainerId, details.ClientId)
            : new WorkoutAssignedDomainEvent(assignment.Id, details.TrainerId, details.ClientId);

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
