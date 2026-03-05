using Microsoft.EntityFrameworkCore;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Enums;
using WeeklyPlanTracker.Core.Interfaces;
using WeeklyPlanTracker.Infrastructure.Data;
using WeeklyPlanTracker.Core.Entities;

namespace WeeklyPlanTracker.Infrastructure.Services;

/// <summary>
/// Handles progress updates and builds the lead's team dashboard aggregates.
/// </summary>
public class ProgressService : IProgressService
{
    private readonly AppDbContext _db;

    public ProgressService(AppDbContext db) => _db = db;

    public async Task<TeamProgressDto> GetTeamProgressAsync(Guid weekId)
    {
        var plans = await _db.MemberPlans
            .Include(mp => mp.Member)
            .Include(mp => mp.TaskAssignments)
                .ThenInclude(ta => ta.BacklogItem)
            .Where(mp => mp.PlanningWeekId == weekId)
            .ToListAsync();

        var allocations = await _db.CategoryAllocations.Where(a => a.PlanningWeekId == weekId).ToListAsync();
        var allTasks = plans.SelectMany(mp => mp.TaskAssignments).ToList();

        return new TeamProgressDto
        {
            WeekId = weekId,
            TotalTasks = allTasks.Count,
            CompletedTasks = allTasks.Count(t => t.ProgressStatus == ProgressStatus.Completed),
            BlockedTasks = allTasks.Count(t => t.ProgressStatus == ProgressStatus.Blocked),
            InProgressTasks = allTasks.Count(t => t.ProgressStatus == ProgressStatus.InProgress),
            TotalCommittedHours = allTasks.Sum(t => t.CommittedHours),
            TotalHoursCompleted = allTasks.Sum(t => t.HoursCompleted),
            ByCategory = BuildCategoryBreakdown(plans, allocations),
            ByMember = plans.Select(mp => BuildMemberProgress(mp)).ToList()
        };
    }

    public async Task<MemberProgressDto> GetMemberProgressAsync(Guid weekId, Guid memberId)
    {
        var plan = await _db.MemberPlans
            .Include(mp => mp.Member)
            .Include(mp => mp.TaskAssignments)
                .ThenInclude(ta => ta.BacklogItem)
            .FirstOrDefaultAsync(mp => mp.PlanningWeekId == weekId && mp.MemberId == memberId)
            ?? throw new InvalidOperationException("Member plan not found.");

        return BuildMemberProgress(plan);
    }

    public async Task<List<ProgressUpdateDto>> GetTaskHistoryAsync(Guid assignmentId)
    {
        var updates = await _db.ProgressUpdates
            .Include(p => p.TaskAssignment)
                .ThenInclude(ta => ta.MemberPlan)
                    .ThenInclude(mp => mp.Member)
            .Where(p => p.TaskAssignmentId == assignmentId)
            .OrderByDescending(p => p.Timestamp)
            .ToListAsync();

        return updates.Select(p => new ProgressUpdateDto
        {
            Id = p.Id,
            TaskAssignmentId = p.TaskAssignmentId,
            UpdatedBy = p.UpdatedBy,
            UpdatedByName = p.TaskAssignment?.MemberPlan?.Member?.Name ?? string.Empty,
            PreviousHoursCompleted = p.PreviousHoursCompleted,
            NewHoursCompleted = p.NewHoursCompleted,
            PreviousStatus = p.PreviousStatus,
            NewStatus = p.NewStatus,
            Note = p.Note,
            Timestamp = p.Timestamp
        }).ToList();
    }

    public async Task<TaskAssignmentDto> SubmitUpdateAsync(Guid assignmentId, SubmitProgressUpdateDto dto)
    {
        var assignment = await _db.TaskAssignments
            .Include(ta => ta.BacklogItem)
            .FirstOrDefaultAsync(ta => ta.Id == assignmentId)
            ?? throw new InvalidOperationException("Task assignment not found.");

        // Record audit entry before changing values
        var update = new ProgressUpdate
        {
            TaskAssignmentId = assignmentId,
            UpdatedBy = dto.UpdatedBy,
            PreviousHoursCompleted = assignment.HoursCompleted,
            NewHoursCompleted = dto.HoursCompleted,
            PreviousStatus = assignment.ProgressStatus,
            NewStatus = dto.Status,
            Note = dto.Note
        };

        // Apply changes to the assignment
        assignment.HoursCompleted = dto.HoursCompleted;
        assignment.ProgressStatus = dto.Status;

        _db.ProgressUpdates.Add(update);
        await _db.SaveChangesAsync();

        return new TaskAssignmentDto
        {
            Id = assignment.Id,
            BacklogItemId = assignment.BacklogItemId,
            BacklogItemTitle = assignment.BacklogItem.Title,
            BacklogItemDescription = assignment.BacklogItem.Description,
            Category = assignment.BacklogItem.Category,
            CategoryLabel = GetCategoryLabel(assignment.BacklogItem.Category),
            CommittedHours = assignment.CommittedHours,
            HoursCompleted = assignment.HoursCompleted,
            ProgressStatus = assignment.ProgressStatus,
            ProgressStatusLabel = GetStatusLabel(assignment.ProgressStatus)
        };
    }

    private static List<CategoryProgressDto> BuildCategoryBreakdown(List<MemberPlan> plans, List<CategoryAllocation> allocations)
    {
        var tasksWithMember = plans
            .SelectMany(mp => mp.TaskAssignments.Select(t => (Task: t, MemberName: mp.Member?.Name ?? string.Empty)))
            .ToList();
        return tasksWithMember
            .GroupBy(x => x.Task.BacklogItem.Category)
            .Select(g => new CategoryProgressDto
            {
                Category = g.Key.ToString(),
                CategoryLabel = GetCategoryLabel(g.Key),
                BudgetHours = allocations.FirstOrDefault(a => a.Category == g.Key)?.BudgetHours ?? 0,
                TotalTasks = g.Count(),
                CompletedTasks = g.Count(x => x.Task.ProgressStatus == ProgressStatus.Completed),
                CommittedHours = g.Sum(x => x.Task.CommittedHours),
                HoursCompleted = g.Sum(x => x.Task.HoursCompleted),
                Tasks = g.Select(x => new TaskAssignmentDto
                {
                    Id = x.Task.Id,
                    BacklogItemId = x.Task.BacklogItemId,
                    BacklogItemTitle = x.Task.BacklogItem.Title,
                    BacklogItemDescription = x.Task.BacklogItem.Description,
                    Category = x.Task.BacklogItem.Category,
                    CategoryLabel = GetCategoryLabel(x.Task.BacklogItem.Category),
                    CommittedHours = x.Task.CommittedHours,
                    HoursCompleted = x.Task.HoursCompleted,
                    ProgressStatus = x.Task.ProgressStatus,
                    ProgressStatusLabel = GetStatusLabel(x.Task.ProgressStatus),
                    MemberName = x.MemberName
                }).ToList()
            })
            .ToList();
    }

    private static MemberProgressDto BuildMemberProgress(MemberPlan mp) => new()
    {
        MemberId = mp.MemberId,
        MemberName = mp.Member?.Name ?? string.Empty,
        TotalTasks = mp.TaskAssignments.Count,
        CompletedTasks = mp.TaskAssignments.Count(t => t.ProgressStatus == ProgressStatus.Completed),
        BlockedTasks = mp.TaskAssignments.Count(t => t.ProgressStatus == ProgressStatus.Blocked),
        CommittedHours = mp.TaskAssignments.Sum(t => t.CommittedHours),
        HoursCompleted = mp.TaskAssignments.Sum(t => t.HoursCompleted),
        Tasks = mp.TaskAssignments.Select(t => new TaskAssignmentDto
        {
            Id = t.Id,
            BacklogItemId = t.BacklogItemId,
            BacklogItemTitle = t.BacklogItem.Title,
            BacklogItemDescription = t.BacklogItem.Description,
            Category = t.BacklogItem.Category,
            CategoryLabel = GetCategoryLabel(t.BacklogItem.Category),
            CommittedHours = t.CommittedHours,
            HoursCompleted = t.HoursCompleted,
            ProgressStatus = t.ProgressStatus,
            ProgressStatusLabel = GetStatusLabel(t.ProgressStatus),
            MemberName = mp.Member?.Name ?? string.Empty
        }).ToList()
    };

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
}