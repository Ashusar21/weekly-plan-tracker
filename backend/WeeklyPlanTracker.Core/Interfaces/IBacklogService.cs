using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Enums;

namespace WeeklyPlanTracker.Core.Interfaces;

public interface IBacklogService
{
    Task<List<BacklogItemDto>> GetAllAsync(BacklogItemStatus? status = null, Category? category = null);
    Task<BacklogItemDto?> GetByIdAsync(Guid id);
    Task<BacklogItemDto> CreateAsync(CreateBacklogItemDto dto);
    Task<BacklogItemDto?> UpdateAsync(Guid id, UpdateBacklogItemDto dto);
    Task<bool> ArchiveAsync(Guid id);
}