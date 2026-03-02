using WeeklyPlanTracker.Core.Enums;

namespace WeeklyPlanTracker.Core.Entities;

/// <summary>
/// A unit of work in the backlog pool, categorized into one of three types.
/// </summary>
public class BacklogItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Category Category { get; set; }
    public BacklogItemStatus Status { get; set; } = BacklogItemStatus.Available;
    public double? EstimatedEffort { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
}