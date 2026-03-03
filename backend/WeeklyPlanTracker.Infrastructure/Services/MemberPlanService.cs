using Microsoft.EntityFrameworkCore;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Entities;
using WeeklyPlanTracker.Core.Enums;
using WeeklyPlanTracker.Core.Interfaces;
using WeeklyPlanTracker.Infrastructure.Data;

namespace WeeklyPlanTracker.Infrastructure.Services;

/// <summary>
/// Handles member plan management: claiming items, adjusting hours, marking ready.
/// </summary>
public class MemberPlanService : IMemberPlanService
{
    private readonly AppDbContext _db;

    public MemberPlanService(AppDbContext db) => _db = db;

    public async Task<MemberPlanDto?> GetAsync(Guid weekId, Guid memberId)
    {
        var plan = await GetPlanWithDetails(weekId, memberId);
        return plan is null ? null : ToDto(plan);
    }

    public async Task<TaskAssignmentDto> ClaimItemAsync(Guid weekId, Guid memberId, ClaimBacklogItemDto dto)
    {
        var plan = await GetPlanWithDetails(weekId, memberId)
            ?? throw new InvalidOperationException("Member plan not found.");

        var backlogItem = await _db.BacklogItems.FindAsync(dto.BacklogItemId)
            ?? throw new InvalidOperationException("Backlog item not found.");

        var assignment = new TaskAssignment
        {
            MemberPlanId = plan.Id,
            BacklogItemId = dto.BacklogItemId,
            CommittedHours = dto.CommittedHours
        };

        plan.TaskAssignments.Add(assignment);
        plan.TotalPlannedHours = plan.TaskAssignments.Sum(t => t.CommittedHours);

        _db.TaskAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        return ToAssignmentDto(assignment, backlogItem);
    }

    public async Task<TaskAssignmentDto?> UpdateHoursAsync(
        Guid weekId, Guid memberId, Guid assignmentId, UpdateCommittedHoursDto dto)
    {
        var plan = await GetPlanWithDetails(weekId, memberId);
        if (plan is null) return null;

        var assignment = plan.TaskAssignments.FirstOrDefault(t => t.Id == assignmentId);
        if (assignment is null) return null;

        assignment.CommittedHours = dto.CommittedHours;
        plan.TotalPlannedHours = plan.TaskAssignments.Sum(t => t.CommittedHours);

        await _db.SaveChangesAsync();

        var backlogItem = await _db.BacklogItems.FindAsync(assignment.BacklogItemId);
        return ToAssignmentDto(assignment, backlogItem!);
    }

    public async Task<bool> RemoveItemAsync(Guid weekId, Guid memberId, Guid assignmentId)
    {
        var plan = await GetPlanWithDetails(weekId, memberId);
        if (plan is null) return false;

        var assignment = plan.TaskAssignments.FirstOrDefault(t => t.Id == assignmentId);
        if (assignment is null) return false;

        _db.TaskAssignments.Remove(assignment);
        plan.TotalPlannedHours = plan.TaskAssignments
            .Where(t => t.Id != assignmentId)
            .Sum(t => t.CommittedHours);

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleReadyAsync(Guid weekId, Guid memberId)
    {
        var plan = await _db.MemberPlans
            .FirstOrDefaultAsync(mp => mp.PlanningWeekId == weekId && mp.MemberId == memberId);
        if (plan is null) return false;

        plan.IsReady = !plan.IsReady;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<MemberPlan?> GetPlanWithDetails(Guid weekId, Guid memberId) =>
        await _db.MemberPlans
            .Include(mp => mp.Member)
            .Include(mp => mp.TaskAssignments)
                .ThenInclude(ta => ta.BacklogItem)
            .FirstOrDefaultAsync(mp =>
                mp.PlanningWeekId == weekId && mp.MemberId == memberId);

    private static string GetCategoryLabel(Category c) => c switch
    {
        Category.ClientFocused => "Client Focused",
        Category.TechDebt => "Tech Debt",
        Category.RAndD => "R&D",
        _ => c.ToString()
    };

    private static string GetStatusLabel(ProgressStatus s) => s switch
    {
        ProgressStatus.NotStarted => "Not Started",
        ProgressStatus.InProgress => "In Progress",
        ProgressStatus.Completed => "Completed",
        ProgressStatus.Blocked => "Blocked",
        _ => s.ToString()
    };

    private static TaskAssignmentDto ToAssignmentDto(TaskAssignment ta, BacklogItem item) => new()
    {
        Id = ta.Id,
        BacklogItemId = item.Id,
        BacklogItemTitle = item.Title,
        BacklogItemDescription = item.Description,
        Category = item.Category,
        CategoryLabel = GetCategoryLabel(item.Category),
        CommittedHours = ta.CommittedHours,
        HoursCompleted = ta.HoursCompleted,
        ProgressStatus = ta.ProgressStatus,
        ProgressStatusLabel = GetStatusLabel(ta.ProgressStatus)
    };

    private static MemberPlanDto ToDto(MemberPlan mp) => new()
    {
        Id = mp.Id,
        PlanningWeekId = mp.PlanningWeekId,
        MemberId = mp.MemberId,
        MemberName = mp.Member?.Name ?? string.Empty,
        IsReady = mp.IsReady,
        TotalPlannedHours = mp.TotalPlannedHours,
        TaskAssignments = mp.TaskAssignments
            .Select(ta => ToAssignmentDto(ta, ta.BacklogItem))
            .ToList()
    };
}