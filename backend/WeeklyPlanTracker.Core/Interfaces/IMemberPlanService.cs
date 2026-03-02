using WeeklyPlanTracker.Core.DTOs;

namespace WeeklyPlanTracker.Core.Interfaces;

public interface IMemberPlanService
{
    Task<MemberPlanDto?> GetAsync(Guid weekId, Guid memberId);
    Task<TaskAssignmentDto> ClaimItemAsync(Guid weekId, Guid memberId, ClaimBacklogItemDto dto);
    Task<TaskAssignmentDto?> UpdateHoursAsync(Guid weekId, Guid memberId, Guid assignmentId, UpdateCommittedHoursDto dto);
    Task<bool> RemoveItemAsync(Guid weekId, Guid memberId, Guid assignmentId);
    Task<bool> ToggleReadyAsync(Guid weekId, Guid memberId);
}