using Microsoft.EntityFrameworkCore;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Entities;
using WeeklyPlanTracker.Core.Enums;
using WeeklyPlanTracker.Core.Interfaces;
using WeeklyPlanTracker.Infrastructure.Data;

namespace WeeklyPlanTracker.Infrastructure.Services;

/// <summary>
/// Manages backlog items — create, edit, filter, and archive.
/// </summary>
public class BacklogService : IBacklogService
{
    private readonly AppDbContext _db;

    public BacklogService(AppDbContext db) => _db = db;

    public async Task<List<BacklogItemDto>> GetAllAsync(
        BacklogItemStatus? status = null,
        Category? category = null)
    {
        var query = _db.BacklogItems.AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (category.HasValue)
            query = query.Where(x => x.Category == category.Value);

        return await query
            .OrderBy(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .ToListAsync();
    }

    public async Task<BacklogItemDto?> GetByIdAsync(Guid id)
    {
        var item = await _db.BacklogItems.FindAsync(id);
        return item is null ? null : ToDto(item);
    }

    public async Task<BacklogItemDto> CreateAsync(CreateBacklogItemDto dto)
    {
        var item = new BacklogItem
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Category = dto.Category,
            EstimatedEffort = dto.EstimatedEffort
        };

        _db.BacklogItems.Add(item);
        await _db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<BacklogItemDto?> UpdateAsync(Guid id, UpdateBacklogItemDto dto)
    {
        var item = await _db.BacklogItems.FindAsync(id);
        if (item is null) return null;

        item.Title = dto.Title.Trim();
        item.Description = dto.Description.Trim();
        item.Category = dto.Category;
        item.EstimatedEffort = dto.EstimatedEffort;

        await _db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<bool> ArchiveAsync(Guid id)
    {
        var item = await _db.BacklogItems.FindAsync(id);
        if (item is null) return false;

        item.Status = BacklogItemStatus.Archived;
        await _db.SaveChangesAsync();
        return true;
    }

    private static string GetCategoryLabel(Category c) => c switch
    {
        Category.ClientFocused => "Client Focused",
        Category.TechDebt => "Tech Debt",
        Category.RAndD => "R&D",
        _ => c.ToString()
    };

    private static BacklogItemDto ToDto(BacklogItem x) => new()
    {
        Id = x.Id,
        Title = x.Title,
        Description = x.Description,
        Category = x.Category,
        CategoryLabel = GetCategoryLabel(x.Category),
        Status = x.Status,
        EstimatedEffort = x.EstimatedEffort,
        CreatedAt = x.CreatedAt
    };
}