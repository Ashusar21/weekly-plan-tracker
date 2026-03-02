using WeeklyPlanTracker.Core.Enums;

namespace WeeklyPlanTracker.Core.DTOs;

public class ProgressUpdateDto
{
    public Guid Id { get; set; }
    public Guid TaskAssignmentId { get; set; }
    public Guid UpdatedBy { get; set; }
    public string UpdatedByName { get; set; } = string.Empty;
    public double PreviousHoursCompleted { get; set; }
    public double NewHoursCompleted { get; set; }
    public ProgressStatus PreviousStatus { get; set; }
    public ProgressStatus NewStatus { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class SubmitProgressUpdateDto
{
    public double HoursCompleted { get; set; }
    public ProgressStatus Status { get; set; }
    public string Note { get; set; } = string.Empty;
    public Guid UpdatedBy { get; set; }
}

/// <summary>
/// Aggregated team progress view for the lead dashboard.
/// </summary>
public class TeamProgressDto
{
    public Guid WeekId { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int BlockedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public double TotalCommittedHours { get; set; }
    public double TotalHoursCompleted { get; set; }
    public List<CategoryProgressDto> ByCategory { get; set; } = new();
    public List<MemberProgressDto> ByMember { get; set; } = new();
}

public class CategoryProgressDto
{
    public string Category { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public double CommittedHours { get; set; }
    public double HoursCompleted { get; set; }
    public List<TaskAssignmentDto> Tasks { get; set; } = new();
}

public class MemberProgressDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int BlockedTasks { get; set; }
    public double CommittedHours { get; set; }
    public double HoursCompleted { get; set; }
    public List<TaskAssignmentDto> Tasks { get; set; } = new();
}