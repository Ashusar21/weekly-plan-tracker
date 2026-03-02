using WeeklyPlanTracker.Core.DTOs;

namespace WeeklyPlanTracker.Core.Interfaces;

public interface ITeamMemberService
{
    Task<List<TeamMemberDto>> GetAllAsync();
    Task<TeamMemberDto?> GetByIdAsync(Guid id);
    Task<TeamMemberDto> CreateAsync(CreateTeamMemberDto dto);
    Task<TeamMemberDto?> UpdateAsync(Guid id, UpdateTeamMemberDto dto);
    Task<bool> MakeLeadAsync(Guid id);
    Task<bool> DeactivateAsync(Guid id);
    Task<bool> ReactivateAsync(Guid id);
    Task<bool> AnyExistsAsync();
}