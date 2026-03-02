using WeeklyPlanTracker.Core.Enums;

namespace WeeklyPlanTracker.Core.Entities;

/// <summary>
/// Represents one planning cycle (Tuesday setup → Wed–Mon execution).
/// </summary>
public class PlanningWeek
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly PlanningDate { get; set; }         // Must be a Tuesday
    public DateOnly ExecutionStartDate { get; set; }   // Wednesday
    public DateOnly ExecutionEndDate { get; set; }     // Following Monday
    public WeekState State { get; set; } = WeekState.Setup;
    public int TeamCapacity { get; set; } = 30;        // Hours per member per week
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<CategoryAllocation> CategoryAllocations { get; set; } = new List<CategoryAllocation>();
    public ICollection<MemberPlan> MemberPlans { get; set; } = new List<MemberPlan>();
}