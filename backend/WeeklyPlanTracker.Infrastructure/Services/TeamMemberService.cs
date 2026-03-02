using Microsoft.EntityFrameworkCore;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Entities;
using WeeklyPlanTracker.Core.Interfaces;
using WeeklyPlanTracker.Infrastructure.Data;

namespace WeeklyPlanTracker.Infrastructure.Services;

/// <summary>
/// Handles all team member CRUD and role management operations.
/// </summary>
public class TeamMemberService : ITeamMemberService
{
    private readonly AppDbContext _db;

    public TeamMemberService(AppDbContext db) => _db = db;

    public async Task<List<TeamMemberDto>> GetAllAsync() =>
        await _db.TeamMembers
            .OrderBy(m => m.CreatedAt)
            .Select(m => ToDto(m))
            .ToListAsync();

    public async Task<TeamMemberDto?> GetByIdAsync(Guid id)
    {
        var m = await _db.TeamMembers.FindAsync(id);
        return m is null ? null : ToDto(m);
    }

    public async Task<TeamMemberDto> CreateAsync(CreateTeamMemberDto dto)
    {
        // First member created becomes the lead automatically
        bool anyExists = await _db.TeamMembers.AnyAsync();

        var member = new TeamMember
        {
            Name = dto.Name.Trim(),
            IsLead = !anyExists
        };

        _db.TeamMembers.Add(member);
        await _db.SaveChangesAsync();
        return ToDto(member);
    }

    public async Task<TeamMemberDto?> UpdateAsync(Guid id, UpdateTeamMemberDto dto)
    {
        var member = await _db.TeamMembers.FindAsync(id);
        if (member is null) return null;

        member.Name = dto.Name.Trim();
        await _db.SaveChangesAsync();
        return ToDto(member);
    }

    public async Task<bool> MakeLeadAsync(Guid id)
    {
        var member = await _db.TeamMembers.FindAsync(id);
        if (member is null || !member.IsActive) return false;

        // Remove lead from current lead
        var currentLead = await _db.TeamMembers.FirstOrDefaultAsync(m => m.IsLead);
        if (currentLead is not null) currentLead.IsLead = false;

        member.IsLead = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var member = await _db.TeamMembers.FindAsync(id);
        if (member is null) return false;

        member.IsActive = false;
        member.IsLead = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReactivateAsync(Guid id)
    {
        var member = await _db.TeamMembers.FindAsync(id);
        if (member is null) return false;

        member.IsActive = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AnyExistsAsync() =>
        await _db.TeamMembers.AnyAsync();

    // Maps entity to DTO
    private static TeamMemberDto ToDto(TeamMember m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        IsLead = m.IsLead,
        IsActive = m.IsActive,
        CreatedAt = m.CreatedAt
    };
}