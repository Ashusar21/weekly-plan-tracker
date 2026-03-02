using WeeklyPlanTracker.Core.Enums;

namespace WeeklyPlanTracker.Core.DTOs;

public class BacklogItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Category Category { get; set; }
    public string CategoryLabel { get; set; } = string.Empty;
    public BacklogItemStatus Status { get; set; }
    public double? EstimatedEffort { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateBacklogItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Category Category { get; set; }
    public double? EstimatedEffort { get; set; }
}

public class UpdateBacklogItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Category Category { get; set; }
    public double? EstimatedEffort { get; set; }
}