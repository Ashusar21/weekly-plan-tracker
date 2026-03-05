using FluentAssertions;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Entities;
using WeeklyPlanTracker.Core.Enums;
using WeeklyPlanTracker.Infrastructure.Services;
using WeeklyPlanTracker.Tests.Helpers;

namespace WeeklyPlanTracker.Tests.Services;

public class ProgressServiceTests
{
    private ProgressService CreateService(out WeeklyPlanTracker.Infrastructure.Data.AppDbContext db)
    {
        db = DbContextFactory.Create();
        return new ProgressService(db);
    }

    private static (PlanningWeek week, TeamMember member, MemberPlan plan, BacklogItem item, TaskAssignment assignment)
        SeedBasicWeekWithTask(WeeklyPlanTracker.Infrastructure.Data.AppDbContext db)
    {
        var member = new TeamMember { Name = "Alice" };
        var item = new BacklogItem { Title = "Fix Bug", Category = Category.TechDebt };
        var week = new PlanningWeek
        {
            PlanningDate = new DateOnly(2025, 6, 3),
            ExecutionStartDate = new DateOnly(2025, 6, 4),
            ExecutionEndDate = new DateOnly(2025, 6, 9),
            State = WeekState.Frozen
        };
        var plan = new MemberPlan { PlanningWeekId = week.Id, MemberId = member.Id, Member = member };
        var assignment = new TaskAssignment
        {
            MemberPlanId = plan.Id,
            BacklogItemId = item.Id,
            BacklogItem = item,
            CommittedHours = 8,
            HoursCompleted = 0,
            ProgressStatus = ProgressStatus.NotStarted
        };
        plan.TaskAssignments.Add(assignment);
        week.MemberPlans.Add(plan);

        db.TeamMembers.Add(member);
        db.BacklogItems.Add(item);
        db.PlanningWeeks.Add(week);
        db.SaveChanges();

        return (week, member, plan, item, assignment);
    }

    [Fact]
    public async Task GetTeamProgressAsync_ReturnsCorrectTotals()
    {
        var svc = CreateService(out var db);
        var (week, _, _, _, assignment) = SeedBasicWeekWithTask(db);

        var result = await svc.GetTeamProgressAsync(week.Id);

        result.WeekId.Should().Be(week.Id);
        result.TotalTasks.Should().Be(1);
        result.TotalCommittedHours.Should().Be(8);
        result.TotalHoursCompleted.Should().Be(0);
    }

    [Fact]
    public async Task GetTeamProgressAsync_CountsCompletedTasksCorrectly()
    {
        var svc = CreateService(out var db);
        var (week, _, plan, item, _) = SeedBasicWeekWithTask(db);

        // Add a second completed task
        var item2 = new BacklogItem { Title = "Feature", Category = Category.ClientFocused };
        var assignment2 = new TaskAssignment
        {
            MemberPlanId = plan.Id,
            BacklogItemId = item2.Id,
            BacklogItem = item2,
            CommittedHours = 4,
            HoursCompleted = 4,
            ProgressStatus = ProgressStatus.Completed
        };
        db.BacklogItems.Add(item2);
        db.TaskAssignments.Add(assignment2);
        await db.SaveChangesAsync();

        var result = await svc.GetTeamProgressAsync(week.Id);

        result.CompletedTasks.Should().Be(1);
        result.TotalTasks.Should().Be(2);
    }

    [Fact]
    public async Task GetMemberProgressAsync_ReturnsCorrectMemberData()
    {
        var svc = CreateService(out var db);
        var (week, member, _, _, _) = SeedBasicWeekWithTask(db);

        var result = await svc.GetMemberProgressAsync(week.Id, member.Id);

        result.MemberId.Should().Be(member.Id);
        result.MemberName.Should().Be("Alice");
        result.TotalTasks.Should().Be(1);
        result.CommittedHours.Should().Be(8);
    }

    [Fact]
    public async Task GetMemberProgressAsync_MemberNotFound_ThrowsException()
    {
        var svc = CreateService(out var db);
        var (week, _, _, _, _) = SeedBasicWeekWithTask(db);

        var act = async () => await svc.GetMemberProgressAsync(week.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SubmitUpdateAsync_UpdatesHoursAndStatus()
    {
        var svc = CreateService(out var db);
        var (_, _, _, _, assignment) = SeedBasicWeekWithTask(db);
        var updatedBy = Guid.NewGuid();

        var result = await svc.SubmitUpdateAsync(assignment.Id, new SubmitProgressUpdateDto
        {
            HoursCompleted = 4,
            Status = ProgressStatus.InProgress,
            UpdatedBy = updatedBy,
            Note = "Making progress"
        });

        result.HoursCompleted.Should().Be(4);
        result.ProgressStatus.Should().Be(ProgressStatus.InProgress);
    }

    [Fact]
    public async Task SubmitUpdateAsync_CreatesAuditLogEntry()
    {
        var svc = CreateService(out var db);
        var (_, _, _, _, assignment) = SeedBasicWeekWithTask(db);

        await svc.SubmitUpdateAsync(assignment.Id, new SubmitProgressUpdateDto
        {
            HoursCompleted = 6,
            Status = ProgressStatus.InProgress,
            UpdatedBy = Guid.NewGuid(),
            Note = "Test note"
        });

        db.ProgressUpdates.Should().HaveCount(1);
        var log = db.ProgressUpdates.First();
        log.PreviousHoursCompleted.Should().Be(0);
        log.NewHoursCompleted.Should().Be(6);
        log.Note.Should().Be("Test note");
    }

    [Fact]
    public async Task SubmitUpdateAsync_NotFound_ThrowsException()
    {
        var svc = CreateService(out _);

        var act = async () => await svc.SubmitUpdateAsync(Guid.NewGuid(), new SubmitProgressUpdateDto
        {
            HoursCompleted = 4,
            Status = ProgressStatus.InProgress,
            UpdatedBy = Guid.NewGuid()
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetTaskHistoryAsync_ReturnsHistoryOrderedByTimestampDesc()
    {
        var svc = CreateService(out var db);
        var (_, _, _, _, assignment) = SeedBasicWeekWithTask(db);

        db.ProgressUpdates.AddRange(
            new ProgressUpdate
            {
                TaskAssignmentId = assignment.Id,
                UpdatedBy = Guid.NewGuid(),
                NewHoursCompleted = 2,
                NewStatus = ProgressStatus.InProgress,
                Timestamp = DateTime.UtcNow.AddHours(-2)
            },
            new ProgressUpdate
            {
                TaskAssignmentId = assignment.Id,
                UpdatedBy = Guid.NewGuid(),
                NewHoursCompleted = 6,
                NewStatus = ProgressStatus.Completed,
                Timestamp = DateTime.UtcNow
            }
        );
        await db.SaveChangesAsync();

        var result = await svc.GetTaskHistoryAsync(assignment.Id);

        result.Should().HaveCount(2);
        result[0].NewHoursCompleted.Should().Be(6); // Most recent first
    }
}
