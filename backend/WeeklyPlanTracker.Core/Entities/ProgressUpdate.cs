using WeeklyPlanTracker.Core.Enums;

namespace WeeklyPlanTracker.Core.Entities;

/// <summary>
/// Immutable audit log entry each time a member updates progress on a task.
/// </summary>
public class ProgressUpdate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskAssignmentId { get; set; }
    public Guid UpdatedBy { get; set; }             // TeamMember.Id
    public double PreviousHoursCompleted { get; set; }
    public double NewHoursCompleted { get; set; }
    public ProgressStatus PreviousStatus { get; set; }
    public ProgressStatus NewStatus { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation
    public TaskAssignment TaskAssignment { get; set; } = null!;
}