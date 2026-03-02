using Microsoft.EntityFrameworkCore;
using WeeklyPlanTracker.Core.Entities;
using WeeklyPlanTracker.Core.Enums;

namespace WeeklyPlanTracker.Infrastructure.Data;

/// <summary>
/// EF Core database context. Uses SQLite for portability.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<BacklogItem> BacklogItems => Set<BacklogItem>();
    public DbSet<PlanningWeek> PlanningWeeks => Set<PlanningWeek>();
    public DbSet<CategoryAllocation> CategoryAllocations => Set<CategoryAllocation>();
    public DbSet<MemberPlan> MemberPlans => Set<MemberPlan>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
    public DbSet<ProgressUpdate> ProgressUpdates => Set<ProgressUpdate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TeamMember
        modelBuilder.Entity<TeamMember>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
        });

        // BacklogItem
        modelBuilder.Entity<BacklogItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Category).HasConversion<int>();
            e.Property(x => x.Status).HasConversion<int>();
        });

        // PlanningWeek
        modelBuilder.Entity<PlanningWeek>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.State).HasConversion<int>();
            // Store DateOnly as string in SQLite
            e.Property(x => x.PlanningDate).HasConversion(
                v => v.ToString("yyyy-MM-dd"),
                v => DateOnly.Parse(v));
            e.Property(x => x.ExecutionStartDate).HasConversion(
                v => v.ToString("yyyy-MM-dd"),
                v => DateOnly.Parse(v));
            e.Property(x => x.ExecutionEndDate).HasConversion(
                v => v.ToString("yyyy-MM-dd"),
                v => DateOnly.Parse(v));
        });

        // CategoryAllocation → PlanningWeek
        modelBuilder.Entity<CategoryAllocation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasConversion<int>();
            e.HasOne(x => x.PlanningWeek)
             .WithMany(x => x.CategoryAllocations)
             .HasForeignKey(x => x.PlanningWeekId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // MemberPlan → PlanningWeek + TeamMember
        modelBuilder.Entity<MemberPlan>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.PlanningWeek)
             .WithMany(x => x.MemberPlans)
             .HasForeignKey(x => x.PlanningWeekId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Member)
             .WithMany(x => x.MemberPlans)
             .HasForeignKey(x => x.MemberId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // TaskAssignment → MemberPlan + BacklogItem
        modelBuilder.Entity<TaskAssignment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProgressStatus).HasConversion<int>();
            e.HasOne(x => x.MemberPlan)
             .WithMany(x => x.TaskAssignments)
             .HasForeignKey(x => x.MemberPlanId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.BacklogItem)
             .WithMany(x => x.TaskAssignments)
             .HasForeignKey(x => x.BacklogItemId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ProgressUpdate → TaskAssignment
        modelBuilder.Entity<ProgressUpdate>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PreviousStatus).HasConversion<int>();
            e.Property(x => x.NewStatus).HasConversion<int>();
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasOne(x => x.TaskAssignment)
             .WithMany(x => x.ProgressUpdates)
             .HasForeignKey(x => x.TaskAssignmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}