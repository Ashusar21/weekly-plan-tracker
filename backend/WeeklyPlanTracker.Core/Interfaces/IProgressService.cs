using WeeklyPlanTracker.Core.DTOs;

namespace WeeklyPlanTracker.Core.Interfaces;

public interface IProgressService
{
    Task<TeamProgressDto> GetTeamProgressAsync(Guid weekId);
    Task<MemberProgressDto> GetMemberProgressAsync(Guid weekId, Guid memberId);
    Task<List<ProgressUpdateDto>> GetTaskHistoryAsync(Guid assignmentId);
    Task<TaskAssignmentDto> SubmitUpdateAsync(Guid assignmentId, SubmitProgressUpdateDto dto);
}