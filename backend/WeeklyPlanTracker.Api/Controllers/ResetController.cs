using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeeklyPlanTracker.Infrastructure.Data;

namespace WeeklyPlanTracker.Api.Controllers;

[ApiController]
[Route("api/reset")]
public class ResetController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResetController(AppDbContext db) => _db = db;

    /// <summary>
    /// Wipes all data from the database. Used by the Reset App button.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> ResetAll()
    {
        // Delete in dependency order to avoid FK violations
        _db.ProgressUpdates.RemoveRange(_db.ProgressUpdates);
        _db.TaskAssignments.RemoveRange(_db.TaskAssignments);
        _db.MemberPlans.RemoveRange(_db.MemberPlans);
        _db.CategoryAllocations.RemoveRange(_db.CategoryAllocations);
        _db.PlanningWeeks.RemoveRange(_db.PlanningWeeks);
        _db.BacklogItems.RemoveRange(_db.BacklogItems);
        _db.TeamMembers.RemoveRange(_db.TeamMembers);

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
