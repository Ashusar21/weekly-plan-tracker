using FluentAssertions;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Entities;
using WeeklyPlanTracker.Core.Enums;
using WeeklyPlanTracker.Infrastructure.Services;
using WeeklyPlanTracker.Tests.Helpers;

namespace WeeklyPlanTracker.Tests.Services;

public class BacklogServiceTests
{
    private BacklogService CreateService(out WeeklyPlanTracker.Infrastructure.Data.AppDbContext db)
    {
        db = DbContextFactory.Create();
        return new BacklogService(db);
    }

    [Fact]
    public async Task GetAllAsync_NoFilters_ReturnsAllItems()
    {
        var svc = CreateService(out var db);
        db.BacklogItems.AddRange(
            new BacklogItem { Title = "Task A", Category = Category.ClientFocused },
            new BacklogItem { Title = "Task B", Category = Category.TechDebt }
        );
        await db.SaveChangesAsync();

        var result = await svc.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_FilterByStatus_ReturnsMatchingItems()
    {
        var svc = CreateService(out var db);
        db.BacklogItems.AddRange(
            new BacklogItem { Title = "Active", Category = Category.ClientFocused, Status = BacklogItemStatus.Available },
            new BacklogItem { Title = "Archived", Category = Category.TechDebt, Status = BacklogItemStatus.Archived }
        );
        await db.SaveChangesAsync();

        var result = await svc.GetAllAsync(status: BacklogItemStatus.Available);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Active");
    }

    [Fact]
    public async Task GetAllAsync_FilterByCategory_ReturnsMatchingItems()
    {
        var svc = CreateService(out var db);
        db.BacklogItems.AddRange(
            new BacklogItem { Title = "Client Task", Category = Category.ClientFocused },
            new BacklogItem { Title = "R&D Task", Category = Category.RAndD }
        );
        await db.SaveChangesAsync();

        var result = await svc.GetAllAsync(category: Category.RAndD);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("R&D Task");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingItem_ReturnsItem()
    {
        var svc = CreateService(out var db);
        var item = new BacklogItem { Title = "Task A", Category = Category.TechDebt };
        db.BacklogItems.Add(item);
        await db.SaveChangesAsync();

        var result = await svc.GetByIdAsync(item.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Task A");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var svc = CreateService(out _);

        var result = await svc.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesItem()
    {
        var svc = CreateService(out var db);

        var result = await svc.CreateAsync(new CreateBacklogItemDto
        {
            Title = "  New Task  ",
            Description = "  Some description  ",
            Category = Category.ClientFocused,
            EstimatedEffort = 5.0
        });

        result.Title.Should().Be("New Task");
        result.Description.Should().Be("Some description");
        result.Category.Should().Be(Category.ClientFocused);
        result.EstimatedEffort.Should().Be(5.0);
        result.Status.Should().Be(BacklogItemStatus.Available);
        db.BacklogItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_SetsCategoryLabelCorrectly()
    {
        var svc = CreateService(out _);

        var result = await svc.CreateAsync(new CreateBacklogItemDto
        {
            Title = "Task",
            Description = "",
            Category = Category.TechDebt
        });

        result.CategoryLabel.Should().Be("Tech Debt");
    }

    [Fact]
    public async Task UpdateAsync_ExistingItem_UpdatesFields()
    {
        var svc = CreateService(out var db);
        var item = new BacklogItem { Title = "Old Title", Description = "Old", Category = Category.TechDebt };
        db.BacklogItems.Add(item);
        await db.SaveChangesAsync();

        var result = await svc.UpdateAsync(item.Id, new UpdateBacklogItemDto
        {
            Title = "New Title",
            Description = "New Desc",
            Category = Category.RAndD,
            EstimatedEffort = 8.0
        });

        result.Should().NotBeNull();
        result!.Title.Should().Be("New Title");
        result.Category.Should().Be(Category.RAndD);
        result.CategoryLabel.Should().Be("R&D");
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNull()
    {
        var svc = CreateService(out _);

        var result = await svc.UpdateAsync(Guid.NewGuid(), new UpdateBacklogItemDto
        {
            Title = "X", Description = "", Category = Category.RAndD
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task ArchiveAsync_ExistingItem_SetsStatusToArchived()
    {
        var svc = CreateService(out var db);
        var item = new BacklogItem { Title = "Task", Category = Category.ClientFocused };
        db.BacklogItems.Add(item);
        await db.SaveChangesAsync();

        var result = await svc.ArchiveAsync(item.Id);

        result.Should().BeTrue();
        db.BacklogItems.Find(item.Id)!.Status.Should().Be(BacklogItemStatus.Archived);
    }

    [Fact]
    public async Task ArchiveAsync_NotFound_ReturnsFalse()
    {
        var svc = CreateService(out _);

        var result = await svc.ArchiveAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }
}
