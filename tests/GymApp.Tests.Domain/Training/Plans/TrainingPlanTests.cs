using FluentAssertions;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.Events;
using GymApp.Modules.Training.Domain.ValueObjects;
using Xunit;
using static GymApp.Tests.Domain.Training.Plans.PlanSamples;

namespace GymApp.Tests.Domain.Training.Plans;

public class TrainingPlanTests
{
    private static readonly Guid Gym = Guid.NewGuid();
    private static readonly Guid Coach = Guid.NewGuid();
    private static readonly Guid Trainee = Guid.NewGuid();

    private static TrainingPlan AssignFrom(PlanTemplate template) =>
        TrainingPlan.Assign(new PlanAssignment(Gym, Coach, Trainee, new DateOnly(2026, 11, 23), template.Id), template.Content);

    [Fact]
    public void Editing_a_plan_copied_from_a_template_never_changes_the_template()
    {
        var template = PlanTemplate.CreateForGym(new TemplateUser(Gym, Guid.NewGuid(), true), Content(FullBodyWeek().CopyFirstWeekToAll(4)));
        var before = System.Text.Json.JsonSerializer.Serialize(template.Document);
        var plan = AssignFrom(template);

        var edited = plan.Document with { Weeks = plan.Document.Weeks.Take(2).ToList() };
        plan.Revise(Content(edited, "Changed for this trainee"), Coach, expectedVersion: 1).IsSuccess.Should().BeTrue();

        template.Name.Should().Be("Beginner full body");
        template.DurationWeeks.Should().Be(4);
        System.Text.Json.JsonSerializer.Serialize(template.Document).Should().Be(before);
        plan.DurationWeeks.Should().Be(2);
        plan.SourceTemplateId.Should().Be(template.Id);
    }

    [Fact]
    public void Assigning_raises_an_event_and_starts_at_version_one()
    {
        var plan = AssignFrom(PlanTemplate.CreateForGym(new TemplateUser(Gym, Coach, true), Content()));

        plan.Status.Should().Be(TrainingPlanStatus.Active);
        plan.Version.Should().Be(1);
        plan.GetDomainEvents().Should().ContainSingle(e => e is TrainingPlanAssignedDomainEvent);
    }

    [Fact]
    public void Revising_bumps_the_version_and_tells_the_trainee()
    {
        var plan = AssignFrom(PlanTemplate.CreateForGym(new TemplateUser(Gym, Coach, true), Content()));

        plan.Revise(Content(), Coach, expectedVersion: 1).IsSuccess.Should().BeTrue();

        plan.Version.Should().Be(2);
        plan.GetDomainEvents().OfType<TrainingPlanUpdatedDomainEvent>().Single().Version.Should().Be(2);
    }

    [Fact]
    public void A_stale_edit_is_rejected_as_a_conflict()
    {
        var plan = AssignFrom(PlanTemplate.CreateForGym(new TemplateUser(Gym, Coach, true), Content()));
        plan.Revise(Content(), Coach, expectedVersion: 1);

        var stale = plan.Revise(Content(), Coach, expectedVersion: 1);

        stale.Error.Code.Should().Be("Training.TrainingPlan.Conflict");
        plan.Version.Should().Be(2);
    }

    [Fact]
    public void A_replaced_plan_stays_in_history_and_cannot_be_edited()
    {
        var plan = AssignFrom(PlanTemplate.CreateForGym(new TemplateUser(Gym, Coach, true), Content()));

        plan.Replace();

        plan.Status.Should().Be(TrainingPlanStatus.Replaced);
        plan.EndedAt.Should().NotBeNull();
        plan.Revise(Content(), Coach, null).Error.Code.Should().Be("Training.TrainingPlanNotActive");
    }

    [Fact]
    public void Gym_templates_are_edited_by_managers_and_coach_templates_by_their_coach()
    {
        var gymTemplate = PlanTemplate.CreateForGym(new TemplateUser(Gym, Guid.NewGuid(), true), Content());
        var coachTemplate = PlanTemplate.CreateForCoach(new TemplateUser(Gym, Coach), Content());
        var otherCoach = Guid.NewGuid();

        gymTemplate.CanBeEditedBy(new TemplateUser(Gym, Coach)).Should().BeFalse();
        gymTemplate.CanBeEditedBy(new TemplateUser(Gym, Guid.NewGuid(), true)).Should().BeTrue();
        coachTemplate.CanBeEditedBy(new TemplateUser(Gym, Coach)).Should().BeTrue();
        coachTemplate.CanBeEditedBy(new TemplateUser(Gym, otherCoach, true)).Should().BeFalse();
        coachTemplate.IsVisibleTo(new TemplateUser(Gym, otherCoach)).Should().BeFalse();
        gymTemplate.IsVisibleTo(new TemplateUser(Gym, otherCoach)).Should().BeTrue();
    }
}
