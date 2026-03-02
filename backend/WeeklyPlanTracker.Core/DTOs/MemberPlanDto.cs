using WeeklyPlanTracker.Core.Enums;

namespace WeeklyPlanTracker.Core.DTOs;

public class MemberPlanDto
{
    public Guid Id { get; set; }
    public Guid PlanningWeekId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public double TotalPlannedHours { get; set; }
    public List<TaskAssignmentDto> TaskAssignments { get; set; } = new();
}

public class TaskAssignmentDto
{
    public Guid Id { get; set; }
    public Guid BacklogItemId { get; set; }
    public string BacklogItemTitle { get; set; } = string.Empty;
    public string BacklogItemDescription { get; set; } = string.Empty;
    public Category Category { get; set; }
    public string CategoryLabel { get; set; } = string.Empty;
    public double CommittedHours { get; set; }
    public double HoursCompleted { get; set; }
    public ProgressStatus ProgressStatus { get; set; }
    public string ProgressStatusLabel { get; set; } = string.Empty;
}

public class ClaimBacklogItemDto
{
    public Guid BacklogItemId { get; set; }
    public double CommittedHours { get; set; }
}

public class UpdateCommittedHoursDto
{
    public double CommittedHours { get; set; }
}