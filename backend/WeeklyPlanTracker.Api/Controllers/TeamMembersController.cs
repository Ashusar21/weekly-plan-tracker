using Microsoft.AspNetCore.Mvc;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Interfaces;

namespace WeeklyPlanTracker.Api.Controllers;

[ApiController]
[Route("api/team-members")]
public class TeamMembersController : ControllerBase
{
    private readonly ITeamMemberService _service;

    public TeamMembersController(ITeamMemberService service) => _service = service;

    /// <summary>Returns all team members.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _service.GetAllAsync());

    /// <summary>Returns a single member by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var member = await _service.GetByIdAsync(id);
        return member is null ? NotFound() : Ok(member);
    }

    /// <summary>Creates a new team member.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamMemberDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates a member's name.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamMemberDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Promotes a member to team lead (demotes current lead).</summary>
    [HttpPatch("{id:guid}/make-lead")]
    public async Task<IActionResult> MakeLead(Guid id)
    {
        var success = await _service.MakeLeadAsync(id);
        return success ? NoContent() : NotFound();
    }

    /// <summary>Deactivates a team member.</summary>
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var success = await _service.DeactivateAsync(id);
        return success ? NoContent() : NotFound();
    }

    /// <summary>Reactivates a previously deactivated member.</summary>
    [HttpPatch("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var success = await _service.ReactivateAsync(id);
        return success ? NoContent() : NotFound();
    }

    /// <summary>Returns true if any members exist (used for first-time setup check).</summary>
    [HttpGet("any")]
    public async Task<IActionResult> AnyExists() =>
        Ok(new { exists = await _service.AnyExistsAsync() });
}