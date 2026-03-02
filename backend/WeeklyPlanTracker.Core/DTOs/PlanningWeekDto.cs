using WeeklyPlanTracker.Core.Enums;

namespace WeeklyPlanTracker.Core.DTOs;

public class PlanningWeekDto
{
    public Guid Id { get; set; }
    public DateOnly PlanningDate { get; set; }
    public DateOnly ExecutionStartDate { get; set; }
    public DateOnly ExecutionEndDate { get; set; }
    public WeekState State { get; set; }
    public int TeamCapacity { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CategoryAllocationDto> CategoryAllocations { get; set; } = new();
    public List<Guid> ParticipatingMemberIds { get; set; } = new();
}

public class CategoryAllocationDto
{
    public Category Category { get; set; }
    public string CategoryLabel { get; set; } = string.Empty;
    public int Percentage { get; set; }
    public double BudgetHours { get; set; }
}

public class CreatePlanningWeekDto
{
    /// <summary>Must be a Tuesday.</summary>
    public DateOnly PlanningDate { get; set; }
    public List<Guid> ParticipatingMemberIds { get; set; } = new();

    /// <summary>Percentages for Client, TechDebt, RAndD — must sum to 100.</summary>
    public int ClientFocusedPercent { get; set; }
    public int TechDebtPercent { get; set; }
    public int RAndDPercent { get; set; }
}

public class UpdateAllocationsDto
{
    public int ClientFocusedPercent { get; set; }
    public int TechDebtPercent { get; set; }
    public int RAndDPercent { get; set; }
    public List<Guid> ParticipatingMemberIds { get; set; } = new();
}