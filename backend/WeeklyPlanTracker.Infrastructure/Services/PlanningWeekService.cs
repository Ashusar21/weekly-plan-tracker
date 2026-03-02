using Microsoft.EntityFrameworkCore;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Entities;
using WeeklyPlanTracker.Core.Enums;
using WeeklyPlanTracker.Core.Interfaces;
using WeeklyPlanTracker.Infrastructure.Data;

namespace WeeklyPlanTracker.Infrastructure.Services;

/// <summary>
/// Manages the full planning week lifecycle: Setup → Planning → Frozen → Completed.
/// </summary>
public class PlanningWeekService : IPlanningWeekService
{
    private readonly AppDbContext _db;

    public PlanningWeekService(AppDbContext db) => _db = db;

    public async Task<List<PlanningWeekDto>> GetAllAsync() =>
        await _db.PlanningWeeks
            .Include(w => w.CategoryAllocations)
            .Include(w => w.MemberPlans)
            .OrderByDescending(w => w.PlanningDate)
            .Select(w => ToDto(w))
            .ToListAsync();

    public async Task<PlanningWeekDto?> GetActiveAsync()
    {
        var week = await _db.PlanningWeeks
            .Include(w => w.CategoryAllocations)
            .Include(w => w.MemberPlans)
            .FirstOrDefaultAsync(w =>
                w.State == WeekState.Setup ||
                w.State == WeekState.Planning ||
                w.State == WeekState.Frozen);

        return week is null ? null : ToDto(week);
    }

    public async Task<PlanningWeekDto?> GetByIdAsync(Guid id)
    {
        var week = await _db.PlanningWeeks
            .Include(w => w.CategoryAllocations)
            .Include(w => w.MemberPlans)
            .FirstOrDefaultAsync(w => w.Id == id);

        return week is null ? null : ToDto(week);
    }

    public async Task<PlanningWeekDto> CreateAsync(CreatePlanningWeekDto dto)
    {
        // Execution runs Wed–Mon following the planning Tuesday
        var execStart = dto.PlanningDate.AddDays(1);
        var execEnd = dto.PlanningDate.AddDays(6);
        int memberCount = dto.ParticipatingMemberIds.Count;
        int totalCapacity = memberCount * 30;

        var week = new PlanningWeek
        {
            PlanningDate = dto.PlanningDate,
            ExecutionStartDate = execStart,
            ExecutionEndDate = execEnd,
            TeamCapacity = totalCapacity,
            State = WeekState.Setup
        };

        // Create category allocations
        week.CategoryAllocations = new List<CategoryAllocation>
        {
            new() { Category = Category.ClientFocused, Percentage = dto.ClientFocusedPercent,
                    BudgetHours = totalCapacity * dto.ClientFocusedPercent / 100.0 },
            new() { Category = Category.TechDebt, Percentage = dto.TechDebtPercent,
                    BudgetHours = totalCapacity * dto.TechDebtPercent / 100.0 },
            new() { Category = Category.RAndD, Percentage = dto.RAndDPercent,
                    BudgetHours = totalCapacity * dto.RAndDPercent / 100.0 }
        };

        // Create a member plan slot for each participating member
        week.MemberPlans = dto.ParticipatingMemberIds
            .Select(mid => new MemberPlan { MemberId = mid, TotalPlannedHours = 0 })
            .ToList();

        _db.PlanningWeeks.Add(week);
        await _db.SaveChangesAsync();
        return ToDto(week);
    }

    public async Task<PlanningWeekDto?> UpdateAllocationsAsync(Guid id, UpdateAllocationsDto dto)
    {
        var week = await _db.PlanningWeeks
            .Include(w => w.CategoryAllocations)
            .Include(w => w.MemberPlans)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (week is null || week.State != WeekState.Setup) return null;

        int memberCount = dto.ParticipatingMemberIds.Count;
        int totalCapacity = memberCount * 30;
        week.TeamCapacity = totalCapacity;

        // Update allocations
        UpdateAllocation(week, Category.ClientFocused, dto.ClientFocusedPercent, totalCapacity);
        UpdateAllocation(week, Category.TechDebt, dto.TechDebtPercent, totalCapacity);
        UpdateAllocation(week, Category.RAndD, dto.RAndDPercent, totalCapacity);

        // Sync member plans — add new, keep existing
        var existingMemberIds = week.MemberPlans.Select(mp => mp.MemberId).ToHashSet();
        foreach (var mid in dto.ParticipatingMemberIds.Where(mid => !existingMemberIds.Contains(mid)))
            week.MemberPlans.Add(new MemberPlan { MemberId = mid, PlanningWeekId = id });

        // Remove members no longer participating
        var toRemove = week.MemberPlans
            .Where(mp => !dto.ParticipatingMemberIds.Contains(mp.MemberId))
            .ToList();
        _db.MemberPlans.RemoveRange(toRemove);

        await _db.SaveChangesAsync();
        return ToDto(week);
    }

    public async Task<bool> OpenPlanningAsync(Guid id)
    {
        var week = await _db.PlanningWeeks.FindAsync(id);
        if (week is null || week.State != WeekState.Setup) return false;

        week.State = WeekState.Planning;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? Error)> FreezeAsync(Guid id)
    {
        var week = await _db.PlanningWeeks
            .Include(w => w.CategoryAllocations)
            .Include(w => w.MemberPlans)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (week is null || week.State != WeekState.Planning)
            return (false, "Week is not in planning state.");

        // All members must be marked ready
        var notReady = week.MemberPlans.Where(mp => !mp.IsReady).ToList();
        if (notReady.Any())
            return (false, $"{notReady.Count} member(s) have not marked themselves as ready.");

        // All category percentages must sum to 100
        int total = week.CategoryAllocations.Sum(a => a.Percentage);
        if (total != 100)
            return (false, $"Category percentages sum to {total}, must equal 100.");

        week.State = WeekState.Frozen;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> FinishAsync(Guid id)
    {
        var week = await _db.PlanningWeeks.FindAsync(id);
        if (week is null || week.State != WeekState.Frozen) return false;

        week.State = WeekState.Completed;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var week = await _db.PlanningWeeks
            .Include(w => w.MemberPlans)
                .ThenInclude(mp => mp.TaskAssignments)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (week is null || week.State == WeekState.Frozen || week.State == WeekState.Completed)
            return false;

        _db.PlanningWeeks.Remove(week);
        await _db.SaveChangesAsync();
        return true;
    }

    // Helper to update a single category allocation in place
    private static void UpdateAllocation(PlanningWeek week, Category cat, int pct, int totalCapacity)
    {
        var alloc = week.CategoryAllocations.FirstOrDefault(a => a.Category == cat);
        if (alloc is not null)
        {
            alloc.Percentage = pct;
            alloc.BudgetHours = totalCapacity * pct / 100.0;
        }
    }

    private static string GetCategoryLabel(Category c) => c switch
    {
        Category.ClientFocused => "Client Focused",
        Category.TechDebt => "Tech Debt",
        Category.RAndD => "R&D",
        _ => c.ToString()
    };

    private static PlanningWeekDto ToDto(PlanningWeek w) => new()
    {
        Id = w.Id,
        PlanningDate = w.PlanningDate,
        ExecutionStartDate = w.ExecutionStartDate,
        ExecutionEndDate = w.ExecutionEndDate,
        State = w.State,
        TeamCapacity = w.TeamCapacity,
        CreatedAt = w.CreatedAt,
        CategoryAllocations = w.CategoryAllocations.Select(a => new CategoryAllocationDto
        {
            Category = a.Category,
            CategoryLabel = GetCategoryLabel(a.Category),
            Percentage = a.Percentage,
            BudgetHours = a.BudgetHours
        }).ToList(),
        ParticipatingMemberIds = w.MemberPlans.Select(mp => mp.MemberId).ToList()
    };
}