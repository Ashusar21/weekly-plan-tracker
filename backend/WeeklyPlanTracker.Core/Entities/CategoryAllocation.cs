using WeeklyPlanTracker.Core.Enums;

namespace WeeklyPlanTracker.Core.Entities;

/// <summary>
/// Stores the lead's percentage split for each category in a given week.
/// </summary>
public class CategoryAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanningWeekId { get; set; }
    public Category Category { get; set; }
    public int Percentage { get; set; }             // 0–100; all 3 must sum to 100
    public double BudgetHours { get; set; }         // Calculated: (Percentage / 100) * TeamCapacity * MemberCount

    // Navigation
    public PlanningWeek PlanningWeek { get; set; } = null!;
}