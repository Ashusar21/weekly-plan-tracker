using Microsoft.AspNetCore.Mvc;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Interfaces;

namespace WeeklyPlanTracker.Api.Controllers;

[ApiController]
[Route("api/progress")]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _service;

    public ProgressController(IProgressService service) => _service = service;

    /// <summary>Returns the full team progress dashboard for a week (lead view).</summary>
    [HttpGet("{weekId:guid}")]
    public async Task<IActionResult> GetTeamProgress(Guid weekId)
    {
        try
        {
            return Ok(await _service.GetTeamProgressAsync(weekId));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Returns a single member's progress for a week.</summary>
    [HttpGet("{weekId:guid}/{memberId:guid}")]
    public async Task<IActionResult> GetMemberProgress(Guid weekId, Guid memberId)
    {
        try
        {
            return Ok(await _service.GetMemberProgressAsync(weekId, memberId));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Returns the full update history for a specific task assignment.</summary>
    [HttpGet("assignments/{assignmentId:guid}/history")]
    public async Task<IActionResult> GetTaskHistory(Guid assignmentId) =>
        Ok(await _service.GetTaskHistoryAsync(assignmentId));

    /// <summary>Submits a progress update for a task (hours done + status + note).</summary>
    [HttpPost("assignments/{assignmentId:guid}")]
    public async Task<IActionResult> SubmitUpdate(
        Guid assignmentId, [FromBody] SubmitProgressUpdateDto dto)
    {
        try
        {
            return Ok(await _service.SubmitUpdateAsync(assignmentId, dto));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}