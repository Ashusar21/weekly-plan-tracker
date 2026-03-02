using Microsoft.AspNetCore.Mvc;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Interfaces;

namespace WeeklyPlanTracker.Api.Controllers;

[ApiController]
[Route("api/planning-weeks")]
public class PlanningWeeksController : ControllerBase
{
    private readonly IPlanningWeekService _service;

    public PlanningWeeksController(IPlanningWeekService service) => _service = service;

    /// <summary>Returns all planning weeks (completed ones = past weeks).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _service.GetAllAsync());

    /// <summary>Returns the currently active week (Setup/Planning/Frozen), if any.</summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var week = await _service.GetActiveAsync();
        return week is null ? NoContent() : Ok(week);
    }

    /// <summary>Returns a specific week by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var week = await _service.GetByIdAsync(id);
        return week is null ? NotFound() : Ok(week);
    }

    /// <summary>Creates a new planning week (lead only).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanningWeekDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates category allocations and member list (Setup state only).</summary>
    [HttpPut("{id:guid}/allocations")]
    public async Task<IActionResult> UpdateAllocations(Guid id, [FromBody] UpdateAllocationsDto dto)
    {
        var updated = await _service.UpdateAllocationsAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Moves week from Setup → Planning (opens member planning).</summary>
    [HttpPatch("{id:guid}/open")]
    public async Task<IActionResult> Open(Guid id)
    {
        var success = await _service.OpenPlanningAsync(id);
        return success ? NoContent() : BadRequest("Cannot open planning for this week.");
    }

    /// <summary>Freezes the plan — requires all members ready and 100% allocation.</summary>
    [HttpPatch("{id:guid}/freeze")]
    public async Task<IActionResult> Freeze(Guid id)
    {
        var (success, error) = await _service.FreezeAsync(id);
        return success ? NoContent() : BadRequest(new { error });
    }

    /// <summary>Completes a frozen week and archives it.</summary>
    [HttpPatch("{id:guid}/finish")]
    public async Task<IActionResult> Finish(Guid id)
    {
        var success = await _service.FinishAsync(id);
        return success ? NoContent() : BadRequest("Cannot finish this week.");
    }

    /// <summary>Cancels and deletes a week (Setup/Planning only).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var success = await _service.CancelAsync(id);
        return success ? NoContent() : BadRequest("Cannot cancel a frozen or completed week.");
    }
}