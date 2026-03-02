namespace WeeklyPlanTracker.Core.Entities;

/// <summary>
/// Represents a team member who can be a lead or a regular member.
/// </summary>
public class TeamMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public bool IsLead { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<MemberPlan> MemberPlans { get; set; } = new List<MemberPlan>();
}