using WeeklyPlanTracker.Core.DTOs;

namespace WeeklyPlanTracker.Core.Interfaces;

public interface IPlanningWeekService
{
    Task<List<PlanningWeekDto>> GetAllAsync();
    Task<PlanningWeekDto?> GetActiveAsync();
    Task<PlanningWeekDto?> GetByIdAsync(Guid id);
    Task<PlanningWeekDto> CreateAsync(CreatePlanningWeekDto dto);
    Task<PlanningWeekDto?> UpdateAllocationsAsync(Guid id, UpdateAllocationsDto dto);
    Task<bool> OpenPlanningAsync(Guid id);
    Task<(bool Success, string? Error)> FreezeAsync(Guid id);
    Task<bool> FinishAsync(Guid id);
    Task<bool> CancelAsync(Guid id);
}