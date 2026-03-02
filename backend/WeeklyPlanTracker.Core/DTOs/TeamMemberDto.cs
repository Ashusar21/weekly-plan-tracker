namespace WeeklyPlanTracker.Core.DTOs;

public class TeamMemberDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsLead { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTeamMemberDto
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateTeamMemberDto
{
    public string Name { get; set; } = string.Empty;
}