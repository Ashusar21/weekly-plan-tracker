using FluentAssertions;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Entities;
using WeeklyPlanTracker.Core.Enums;
using WeeklyPlanTracker.Infrastructure.Services;
using WeeklyPlanTracker.Tests.Helpers;

namespace WeeklyPlanTracker.Tests.Services;

public class PlanningCycleServiceTests
{
    private PlanningWeekService CreateService(out WeeklyPlanTracker.Infrastructure.Data.AppDbContext db)
    {
        db = DbContextFactory.Create();
        return new PlanningWeekService(db);
    }

    private static CreatePlanningWeekDto MakeCreateDto(
        DateOnly? planningDate = null,
        List<Guid>? memberIds = null) => new()
    {
        PlanningDate = planningDate ?? new DateOnly(2025, 6, 3), // Tuesday
        ClientFocusedPercent = 50,
        TechDebtPercent = 30,
        RAndDPercent = 20,
        ParticipatingMemberIds = memberIds ?? [Guid.NewGuid(), Guid.NewGuid()]
    };

    [Fact]
    public async Task CreateAsync_SetsExecutionDatesCorrectly()
    {
        var svc = CreateService(out _);
        var planningDate = new DateOnly(2025, 6, 3); // Tuesday

        var result = await svc.CreateAsync(MakeCreateDto(planningDate));

        result.ExecutionStartDate.Should().Be(new DateOnly(2025, 6, 4)); // Wednesday
        result.ExecutionEndDate.Should().Be(new DateOnly(2025, 6, 9));   // Monday
    }

    [Fact]
    public async Task CreateAsync_CalculatesTeamCapacityCorrectly()
    {
        var svc = CreateService(out _);
        var memberIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }; // 3 members

        var result = await svc.CreateAsync(MakeCreateDto(memberIds: memberIds));

        result.TeamCapacity.Should().Be(90); // 3 * 30
    }

    [Fact]
    public async Task CreateAsync_CreatesMemberPlanForEachParticipant()
    {
        var svc = CreateService(out _);
        var memberIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var result = await svc.CreateAsync(MakeCreateDto(memberIds: memberIds));

        result.ParticipatingMemberIds.Should().BeEquivalentTo(memberIds);
    }

    [Fact]
    public async Task CreateAsync_StateIsSetup()
    {
        var svc = CreateService(out _);

        var result = await svc.CreateAsync(MakeCreateDto());

        result.State.Should().Be(WeekState.Setup);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllWeeks()
    {
        var svc = CreateService(out var db);
        db.PlanningWeeks.AddRange(
            new PlanningWeek { PlanningDate = new DateOnly(2025, 6, 3), ExecutionStartDate = new DateOnly(2025, 6, 4), ExecutionEndDate = new DateOnly(2025, 6, 9) },
            new PlanningWeek { PlanningDate = new DateOnly(2025, 6, 10), ExecutionStartDate = new DateOnly(2025, 6, 11), ExecutionEndDate = new DateOnly(2025, 6, 16) }
        );
        await db.SaveChangesAsync();

        var result = await svc.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsSetupWeek()
    {
        var svc = CreateService(out var db);
        var week = new PlanningWeek
        {
            PlanningDate = new DateOnly(2025, 6, 3),
            ExecutionStartDate = new DateOnly(2025, 6, 4),
            ExecutionEndDate = new DateOnly(2025, 6, 9),
            State = WeekState.Setup
        };
        db.PlanningWeeks.Add(week);
        await db.SaveChangesAsync();

        var result = await svc.GetActiveAsync();

        result.Should().NotBeNull();
        result!.State.Should().Be(WeekState.Setup);
    }

    [Fact]
    public async Task GetActiveAsync_CompletedWeek_ReturnsNull()
    {
        var svc = CreateService(out var db);
        db.PlanningWeeks.Add(new PlanningWeek
        {
            PlanningDate = new DateOnly(2025, 6, 3),
            ExecutionStartDate = new DateOnly(2025, 6, 4),
            ExecutionEndDate = new DateOnly(2025, 6, 9),
            State = WeekState.Completed
        });
        await db.SaveChangesAsync();

        var result = await svc.GetActiveAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task OpenPlanningAsync_FromSetup_TransitionsToPlanningState()
    {
        var svc = CreateService(out var db);
        var week = new PlanningWeek
        {
            PlanningDate = new DateOnly(2025, 6, 3),
            ExecutionStartDate = new DateOnly(2025, 6, 4),
            ExecutionEndDate = new DateOnly(2025, 6, 9),
            State = WeekState.Setup
        };
        db.PlanningWeeks.Add(week);
        await db.SaveChangesAsync();

        var result = await svc.OpenPlanningAsync(week.Id);

        result.Should().BeTrue();
        db.PlanningWeeks.Find(week.Id)!.State.Should().Be(WeekState.Planning);
    }

    [Fact]
    public async Task OpenPlanningAsync_NotInSetup_ReturnsFalse()
    {
        var svc = CreateService(out var db);
        var week = new PlanningWeek
        {
            PlanningDate = new DateOnly(2025, 6, 3),
            ExecutionStartDate = new DateOnly(2025, 6, 4),
            ExecutionEndDate = new DateOnly(2025, 6, 9),
            State = WeekState.Planning
        };
        db.PlanningWeeks.Add(week);
        await db.SaveChangesAsync();

        var result = await svc.OpenPlanningAsync(week.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task FreezeAsync_AllMembersReady_FreezesSuccessfully()
    {
        var svc = CreateService(out var db);
        var week = new PlanningWeek
        {
            PlanningDate = new DateOnly(2025, 6, 3),
            ExecutionStartDate = new DateOnly(2025, 6, 4),
            ExecutionEndDate = new DateOnly(2025, 6, 9),
            State = WeekState.Planning,
            CategoryAllocations = new List<CategoryAllocation>
            {
                new() { Category = Category.ClientFocused, Percentage = 50 },
                new() { Category = Category.TechDebt, Percentage = 30 },
                new() { Category = Category.RAndD, Percentage = 20 }
            },
            MemberPlans = new List<MemberPlan>
            {
                new() { MemberId = Guid.NewGuid(), IsReady = true }
            }
        };
        db.PlanningWeeks.Add(week);
        await db.SaveChangesAsync();

        var (success, error) = await svc.FreezeAsync(week.Id);

        success.Should().BeTrue();
        error.Should().BeNull();
        db.PlanningWeeks.Find(week.Id)!.State.Should().Be(WeekState.Frozen);
    }

    [Fact]
    public async Task FreezeAsync_MemberNotReady_ReturnsError()
    {
        var svc = CreateService(out var db);
        var week = new PlanningWeek
        {
            PlanningDate = new DateOnly(2025, 6, 3),
            ExecutionStartDate = new DateOnly(2025, 6, 4),
            ExecutionEndDate = new DateOnly(2025, 6, 9),
            State = WeekState.Planning,
            CategoryAllocations = new List<CategoryAllocation>
            {
                new() { Category = Category.ClientFocused, Percentage = 50 },
                new() { Category = Category.TechDebt, Percentage = 30 },
                new() { Category = Category.RAndD, Percentage = 20 }
            },
            MemberPlans = new List<MemberPlan>
            {
                new() { MemberId = Guid.NewGuid(), IsReady = false }
            }
        };
        db.PlanningWeeks.Add(week);
        await db.SaveChangesAsync();

        var (success, error) = await svc.FreezeAsync(week.Id);

        success.Should().BeFalse();
        error.Should().Contain("not marked themselves as ready");
    }

    [Fact]
    public async Task FreezeAsync_PercentagesNotSumTo100_ReturnsError()
    {
        var svc = CreateService(out var db);
        var week = new PlanningWeek
        {
            PlanningDate = new DateOnly(2025, 6, 3),
            ExecutionStartDate = new DateOnly(2025, 6, 4),
            ExecutionEndDate = new DateOnly(2025, 6, 9),
            State = WeekState.Planning,
            CategoryAllocations = new List<CategoryAllocation>
            {
                new() { Category = Category.ClientFocused, Percentage = 40 },
                new() { Category = Category.TechDebt, Percentage = 30 },
                new() { Category = Category.RAndD, Percentage = 20 } // only 90
            },
            MemberPlans = new List<MemberPlan>
            {
                new() { MemberId = Guid.NewGuid(), IsReady = true }
            }
        };
        db.PlanningWeeks.Add(week);
        await db.SaveChangesAsync();

        var (success, error) = await svc.FreezeAsync(week.Id);

        success.Should().BeFalse();
        error.Should().Contain("100");
    }

    [Fact]
    public async Task FinishAsync_FrozenWeek_CompletesSuccessfully()
    {
        var svc = CreateService(out var db);
        var week = new PlanningWeek
        {
            PlanningDate = new DateOnly(2025, 6, 3),
            ExecutionStartDate = new DateOnly(2025, 6, 4),
            ExecutionEndDate = new DateOnly(2025, 6, 9),
            State = WeekState.Frozen
        };
        db.PlanningWeeks.Add(week);
        await db.SaveChangesAsync();

        var result = await svc.FinishAsync(week.Id);

        result.Should().BeTrue();
        db.PlanningWeeks.Find(week.Id)!.State.Should().Be(WeekState.Completed);
    }

    [Fact]
    public async Task CancelAsync_SetupWeek_DeletesWeek()
    {
        var svc = CreateService(out var db);
        var week = new PlanningWeek
        {
            PlanningDate = new DateOnly(2025, 6, 3),
            ExecutionStartDate = new DateOnly(2025, 6, 4),
            ExecutionEndDate = new DateOnly(2025, 6, 9),
            State = WeekState.Setup
        };
        db.PlanningWeeks.Add(week);
        await db.SaveChangesAsync();

        var result = await svc.CancelAsync(week.Id);

        result.Should().BeTrue();
        db.PlanningWeeks.Find(week.Id).Should().BeNull();
    }

    [Fact]
    public async Task CancelAsync_FrozenWeek_ReturnsFalse()
    {
        var svc = CreateService(out var db);
        var week = new PlanningWeek
        {
            PlanningDate = new DateOnly(2025, 6, 3),
            ExecutionStartDate = new DateOnly(2025, 6, 4),
            ExecutionEndDate = new DateOnly(2025, 6, 9),
            State = WeekState.Frozen
        };
        db.PlanningWeeks.Add(week);
        await db.SaveChangesAsync();

        var result = await svc.CancelAsync(week.Id);

        result.Should().BeFalse();
    }
}
