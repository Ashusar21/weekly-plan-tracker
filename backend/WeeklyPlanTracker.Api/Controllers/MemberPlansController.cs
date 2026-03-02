using Microsoft.AspNetCore.Mvc;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Interfaces;

namespace WeeklyPlanTracker.Api.Controllers;

[ApiController]
[Route("api/member-plans")]
public class MemberPlansController : ControllerBase
{
    private readonly IMemberPlanService _service;

    public MemberPlansController(IMemberPlanService service) => _service = service;

    /// <summary>Returns a member's full plan for a given week.</summary>
    [HttpGet("{weekId:guid}/{memberId:guid}")]
    public async Task<IActionResult> Get(Guid weekId, Guid memberId)
    {
        var plan = await _service.GetAsync(weekId, memberId);
        return plan is null ? NotFound() : Ok(plan);
    }

    /// <summary>Adds a backlog item to a member's plan with committed hours.</summary>
    [HttpPost("{weekId:guid}/{memberId:guid}/assignments")]
    public async Task<IActionResult> ClaimItem(
        Guid weekId, Guid memberId, [FromBody] ClaimBacklogItemDto dto)
    {
        try
        {
            var result = await _service.ClaimItemAsync(weekId, memberId, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Updates committed hours for an existing assignment.</summary>
    [HttpPut("{weekId:guid}/{memberId:guid}/assignments/{assignmentId:guid}")]
    public async Task<IActionResult> UpdateHours(
        Guid weekId, Guid memberId, Guid assignmentId,
        [FromBody] UpdateCommittedHoursDto dto)
    {
        var result = await _service.UpdateHoursAsync(weekId, memberId, assignmentId, dto);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Removes a backlog item from a member's plan.</summary>
    [HttpDelete("{weekId:guid}/{memberId:guid}/assignments/{assignmentId:guid}")]
    public async Task<IActionResult> RemoveItem(
        Guid weekId, Guid memberId, Guid assignmentId)
    {
        var success = await _service.RemoveItemAsync(weekId, memberId, assignmentId);
        return success ? NoContent() : NotFound();
    }

    /// <summary>Toggles a member's ready status for the week.</summary>
    [HttpPatch("{weekId:guid}/{memberId:guid}/ready")]
    public async Task<IActionResult> ToggleReady(Guid weekId, Guid memberId)
    {
        var success = await _service.ToggleReadyAsync(weekId, memberId);
        return success ? NoContent() : NotFound();
    }
}