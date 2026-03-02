using WeeklyPlanTracker.Core.Enums;

namespace WeeklyPlanTracker.Core.Entities;

/// <summary>
/// A backlog item claimed by a member in their weekly plan, with committed hours and progress.
/// </summary>
public class TaskAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberPlanId { get; set; }
    public Guid BacklogItemId { get; set; }
    public double CommittedHours { get; set; }
    public double HoursCompleted { get; set; } = 0;
    public ProgressStatus ProgressStatus { get; set; } = ProgressStatus.NotStarted;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public MemberPlan MemberPlan { get; set; } = null!;
    public BacklogItem BacklogItem { get; set; } = null!;
    public ICollection<ProgressUpdate> ProgressUpdates { get; set; } = new List<ProgressUpdate>();
}