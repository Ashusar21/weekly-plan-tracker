using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeeklyPlanTracker.Core.Enums;
using WeeklyPlanTracker.Core.Entities;
using WeeklyPlanTracker.Infrastructure.Data;

namespace WeeklyPlanTracker.Api.Controllers;

[ApiController]
[Route("api/restore")]
public class RestoreController : ControllerBase
{
    private readonly AppDbContext _db;

    public RestoreController(AppDbContext db) => _db = db;

    // GET /api/restore/backup — full raw DB snapshot for export
    [HttpGet("backup")]
    public async Task<IActionResult> Backup()
    {
        var teamMembers = await _db.TeamMembers.ToListAsync();
        var backlogItems = await _db.BacklogItems.ToListAsync();
        var weeks = await _db.PlanningWeeks
            .Include(w => w.CategoryAllocations)
            .Include(w => w.MemberPlans)
                .ThenInclude(mp => mp.TaskAssignments)
                    .ThenInclude(ta => ta.ProgressUpdates)
            .ToListAsync();

        var payload = new
        {
            teamMembers = teamMembers.Select(m => new {
                m.Id, m.Name, m.IsLead, m.IsActive, m.CreatedAt
            }),
            backlogItems = backlogItems.Select(b => new {
                b.Id, b.Title, b.Description, b.Category, b.EstimatedEffort, b.Status, b.CreatedAt
            }),
            planningWeeks = weeks.Select(w => new {
                w.Id, w.PlanningDate, w.ExecutionStartDate, w.ExecutionEndDate,
                w.TeamCapacity, w.State, w.CreatedAt,
                categoryAllocations = w.CategoryAllocations.Select(a => new {
                    a.Id, a.Category, a.Percentage, a.BudgetHours
                }),
                memberPlans = w.MemberPlans.Select(mp => new {
                    mp.Id, mp.MemberId, mp.TotalPlannedHours, mp.IsReady,
                    taskAssignments = mp.TaskAssignments.Select(ta => new {
                        ta.Id, ta.BacklogItemId, ta.CommittedHours, ta.HoursCompleted,
                        ta.ProgressStatus, ta.CreatedAt,
                        progressUpdates = ta.ProgressUpdates.Select(pu => new {
                            pu.Id, pu.UpdatedBy, pu.PreviousHoursCompleted, pu.NewHoursCompleted,
                            pu.PreviousStatus, pu.NewStatus, pu.Note, pu.Timestamp
                        })
                    })
                })
            })
        };

        return Ok(payload);
    }

    // POST /api/restore — wipe + restore from backup
    [HttpPost]
    public async Task<IActionResult> Restore([FromBody] RestorePayload payload)
    {
        _db.ProgressUpdates.RemoveRange(_db.ProgressUpdates);
        _db.TaskAssignments.RemoveRange(_db.TaskAssignments);
        _db.MemberPlans.RemoveRange(_db.MemberPlans);
        _db.CategoryAllocations.RemoveRange(_db.CategoryAllocations);
        _db.PlanningWeeks.RemoveRange(_db.PlanningWeeks);
        _db.BacklogItems.RemoveRange(_db.BacklogItems);
        _db.TeamMembers.RemoveRange(_db.TeamMembers);
        await _db.SaveChangesAsync();

        foreach (var m in payload.TeamMembers)
        {
            _db.TeamMembers.Add(new TeamMember
            {
                Id = m.Id, Name = m.Name, IsLead = m.IsLead,
                IsActive = m.IsActive, CreatedAt = m.CreatedAt
            });
        }

        foreach (var b in payload.BacklogItems)
        {
            _db.BacklogItems.Add(new BacklogItem
            {
                Id = b.Id, Title = b.Title, Description = b.Description,
                Category = b.Category, EstimatedEffort = b.EstimatedEffort,
                Status = b.Status, CreatedAt = b.CreatedAt
            });
        }

        foreach (var w in payload.PlanningWeeks)
        {
            _db.PlanningWeeks.Add(new PlanningWeek
            {
                Id = w.Id,
                PlanningDate = w.PlanningDate,
                ExecutionStartDate = w.ExecutionStartDate,
                ExecutionEndDate = w.ExecutionEndDate,
                TeamCapacity = w.TeamCapacity,
                State = w.State,
                CreatedAt = w.CreatedAt,
                CategoryAllocations = w.CategoryAllocations.Select(a => new CategoryAllocation
                {
                    Id = a.Id, PlanningWeekId = w.Id,
                    Category = a.Category, Percentage = a.Percentage, BudgetHours = a.BudgetHours
                }).ToList(),
                MemberPlans = w.MemberPlans.Select(mp => new MemberPlan
                {
                    Id = mp.Id, PlanningWeekId = w.Id, MemberId = mp.MemberId,
                    TotalPlannedHours = mp.TotalPlannedHours, IsReady = mp.IsReady,
                    TaskAssignments = mp.TaskAssignments.Select(ta => new TaskAssignment
                    {
                        Id = ta.Id, MemberPlanId = mp.Id, BacklogItemId = ta.BacklogItemId,
                        CommittedHours = ta.CommittedHours, HoursCompleted = ta.HoursCompleted,
                        ProgressStatus = ta.ProgressStatus, CreatedAt = ta.CreatedAt,
                        ProgressUpdates = ta.ProgressUpdates.Select(pu => new ProgressUpdate
                        {
                            Id = pu.Id, TaskAssignmentId = ta.Id, UpdatedBy = pu.UpdatedBy,
                            PreviousHoursCompleted = pu.PreviousHoursCompleted,
                            NewHoursCompleted = pu.NewHoursCompleted,
                            PreviousStatus = pu.PreviousStatus, NewStatus = pu.NewStatus,
                            Note = pu.Note, Timestamp = pu.Timestamp
                        }).ToList()
                    }).ToList()
                }).ToList()
            });
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class RestorePayload
{
    public List<RestoreTeamMember> TeamMembers { get; set; } = new();
    public List<RestoreBacklogItem> BacklogItems { get; set; } = new();
    public List<RestorePlanningWeek> PlanningWeeks { get; set; } = new();
}

public class RestoreTeamMember
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsLead { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RestoreBacklogItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Category Category { get; set; }
    public double? EstimatedEffort { get; set; }
    public BacklogItemStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RestorePlanningWeek
{
    public Guid Id { get; set; }
    public DateOnly PlanningDate { get; set; }
    public DateOnly ExecutionStartDate { get; set; }
    public DateOnly ExecutionEndDate { get; set; }
    public int TeamCapacity { get; set; }
    public WeekState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RestoreCategoryAllocation> CategoryAllocations { get; set; } = new();
    public List<RestoreMemberPlan> MemberPlans { get; set; } = new();
}

public class RestoreCategoryAllocation
{
    public Guid Id { get; set; }
    public Category Category { get; set; }
    public int Percentage { get; set; }
    public double BudgetHours { get; set; }
}

public class RestoreMemberPlan
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public double TotalPlannedHours { get; set; }
    public bool IsReady { get; set; }
    public List<RestoreTaskAssignment> TaskAssignments { get; set; } = new();
}

public class RestoreTaskAssignment
{
    public Guid Id { get; set; }
    public Guid BacklogItemId { get; set; }
    public double CommittedHours { get; set; }
    public double HoursCompleted { get; set; }
    public ProgressStatus ProgressStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RestoreProgressUpdate> ProgressUpdates { get; set; } = new();
}

public class RestoreProgressUpdate
{
    public Guid Id { get; set; }
    public Guid UpdatedBy { get; set; }
    public double PreviousHoursCompleted { get; set; }
    public double NewHoursCompleted { get; set; }
    public ProgressStatus PreviousStatus { get; set; }
    public ProgressStatus NewStatus { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}