using FluentAssertions;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Entities;
using WeeklyPlanTracker.Core.Enums;
using WeeklyPlanTracker.Infrastructure.Services;
using WeeklyPlanTracker.Tests.Helpers;

namespace WeeklyPlanTracker.Tests.Services;

public class TaskAssignmentServiceTests
{
    private MemberPlanService CreateService(out WeeklyPlanTracker.Infrastructure.Data.AppDbContext db)
    {
        db = DbContextFactory.Create();
        return new MemberPlanService(db);
    }

    private static (PlanningWeek week, TeamMember member, MemberPlan plan, BacklogItem item)
        SeedPlanWithItem(WeeklyPlanTracker.Infrastructure.Data.AppDbContext db)
    {
        var member = new TeamMember { Name = "Alice" };
        var item = new BacklogItem { Title = "Fix Bug", Category = Category.TechDebt, Status = BacklogItemStatus.Available };
        var week = new PlanningWeek
        {
            PlanningDate = new DateOnly(2025, 6, 3),
            ExecutionStartDate = new DateOnly(2025, 6, 4),
            ExecutionEndDate = new DateOnly(2025, 6, 9),
            State = WeekState.Planning
        };
        var plan = new MemberPlan
        {
            PlanningWeekId = week.Id,
            MemberId = member.Id,
            Member = member,
            IsReady = false
        };
        week.MemberPlans.Add(plan);

        db.TeamMembers.Add(member);
        db.BacklogItems.Add(item);
        db.PlanningWeeks.Add(week);
        db.SaveChanges();

        return (week, member, plan, item);
    }

    [Fact]
    public async Task GetAsync_ExistingPlan_ReturnsMemberPlan()
    {
        var svc = CreateService(out var db);
        var (week, member, _, _) = SeedPlanWithItem(db);

        var result = await svc.GetAsync(week.Id, member.Id);

        result.Should().NotBeNull();
        result!.MemberId.Should().Be(member.Id);
        result.MemberName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetAsync_NotFound_ReturnsNull()
    {
        var svc = CreateService(out _);

        var result = await svc.GetAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task ClaimItemAsync_ValidItem_AddsAssignment()
    {
        var svc = CreateService(out var db);
        var (week, member, _, item) = SeedPlanWithItem(db);

        var result = await svc.ClaimItemAsync(week.Id, member.Id, new ClaimBacklogItemDto
        {
            BacklogItemId = item.Id,
            CommittedHours = 6
        });

        result.BacklogItemId.Should().Be(item.Id);
        result.CommittedHours.Should().Be(6);
        result.ProgressStatus.Should().Be(ProgressStatus.NotStarted);
        db.TaskAssignments.Should().HaveCount(1);
    }

    [Fact]
    public async Task ClaimItemAsync_PlanNotFound_ThrowsException()
    {
        var svc = CreateService(out _);

        var act = async () => await svc.ClaimItemAsync(Guid.NewGuid(), Guid.NewGuid(), new ClaimBacklogItemDto
        {
            BacklogItemId = Guid.NewGuid(),
            CommittedHours = 5
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateHoursAsync_ExistingAssignment_UpdatesHours()
    {
        var svc = CreateService(out var db);
        var (week, member, plan, item) = SeedPlanWithItem(db);

        var assignment = new TaskAssignment
        {
            MemberPlanId = plan.Id,
            BacklogItemId = item.Id,
            CommittedHours = 4
        };
        db.TaskAssignments.Add(assignment);
        await db.SaveChangesAsync();

        var result = await svc.UpdateHoursAsync(week.Id, member.Id, assignment.Id, new UpdateCommittedHoursDto
        {
            CommittedHours = 8
        });

        result.Should().NotBeNull();
        result!.CommittedHours.Should().Be(8);
    }

    [Fact]
    public async Task UpdateHoursAsync_AssignmentNotFound_ReturnsNull()
    {
        var svc = CreateService(out var db);
        var (week, member, _, _) = SeedPlanWithItem(db);

        var result = await svc.UpdateHoursAsync(week.Id, member.Id, Guid.NewGuid(), new UpdateCommittedHoursDto
        {
            CommittedHours = 8
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveItemAsync_ExistingAssignment_RemovesIt()
    {
        var svc = CreateService(out var db);
        var (week, member, plan, item) = SeedPlanWithItem(db);

        var assignment = new TaskAssignment
        {
            MemberPlanId = plan.Id,
            BacklogItemId = item.Id,
            CommittedHours = 4
        };
        db.TaskAssignments.Add(assignment);
        await db.SaveChangesAsync();

        var result = await svc.RemoveItemAsync(week.Id, member.Id, assignment.Id);

        result.Should().BeTrue();
        db.TaskAssignments.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveItemAsync_NotFound_ReturnsFalse()
    {
        var svc = CreateService(out var db);
        var (week, member, _, _) = SeedPlanWithItem(db);

        var result = await svc.RemoveItemAsync(week.Id, member.Id, Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleReadyAsync_NotReady_SetsReady()
    {
        var svc = CreateService(out var db);
        var (week, member, plan, _) = SeedPlanWithItem(db);

        var result = await svc.ToggleReadyAsync(week.Id, member.Id);

        result.Should().BeTrue();
        db.MemberPlans.Find(plan.Id)!.IsReady.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleReadyAsync_AlreadyReady_SetsNotReady()
    {
        var svc = CreateService(out var db);
        var (week, member, plan, _) = SeedPlanWithItem(db);
        plan.IsReady = true;
        await db.SaveChangesAsync();

        var result = await svc.ToggleReadyAsync(week.Id, member.Id);

        result.Should().BeTrue();
        db.MemberPlans.Find(plan.Id)!.IsReady.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleReadyAsync_PlanNotFound_ReturnsFalse()
    {
        var svc = CreateService(out _);

        var result = await svc.ToggleReadyAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeFalse();
    }
}
