using Microsoft.AspNetCore.Mvc;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Enums;
using WeeklyPlanTracker.Core.Interfaces;

namespace WeeklyPlanTracker.Api.Controllers;

[ApiController]
[Route("api/backlog")]
public class BacklogController : ControllerBase
{
    private readonly IBacklogService _service;

    public BacklogController(IBacklogService service) => _service = service;

    /// <summary>Returns all backlog items with optional status/category filters.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] BacklogItemStatus? status,
        [FromQuery] Category? category) =>
        Ok(await _service.GetAllAsync(status, category));

    /// <summary>Returns a single backlog item by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Creates a new backlog item.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBacklogItemDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing backlog item.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBacklogItemDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Archives a backlog item (soft delete).</summary>
    [HttpPatch("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id)
    {
        var success = await _service.ArchiveAsync(id);
        return success ? NoContent() : NotFound();
    }
}