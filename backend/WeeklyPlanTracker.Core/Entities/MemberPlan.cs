namespace WeeklyPlanTracker.Core.Entities;

/// <summary>
/// One member's plan for a specific week. Tracks readiness and total planned hours.
/// </summary>
public class MemberPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanningWeekId { get; set; }
    public Guid MemberId { get; set; }
    public bool IsReady { get; set; } = false;      // Member marked themselves ready
    public double TotalPlannedHours { get; set; } = 0;

    // Navigation
    public PlanningWeek PlanningWeek { get; set; } = null!;
    public TeamMember Member { get; set; } = null!;
    public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
}