namespace WeeklyPlanTracker.Core.Enums;

/// <summary>
/// Lifecycle states of a planning week.
/// </summary>
public enum WeekState
{
    Setup = 1,      // Lead is configuring the week
    Planning = 2,   // Members are picking backlog items
    Frozen = 3,     // Plan is locked; only progress updates allowed
    Completed = 4   // Week is finished and archived
}