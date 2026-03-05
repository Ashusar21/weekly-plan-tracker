using FluentAssertions;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Entities;
using WeeklyPlanTracker.Infrastructure.Services;
using WeeklyPlanTracker.Tests.Helpers;

namespace WeeklyPlanTracker.Tests.Services;

public class TeamMemberServiceTests
{
    private TeamMemberService CreateService(out WeeklyPlanTracker.Infrastructure.Data.AppDbContext db)
    {
        db = DbContextFactory.Create();
        return new TeamMemberService(db);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllMembers()
    {
        var svc = CreateService(out var db);
        db.TeamMembers.AddRange(
            new TeamMember { Name = "Alice" },
            new TeamMember { Name = "Bob" }
        );
        await db.SaveChangesAsync();

        var result = await svc.GetAllAsync();

        result.Should().HaveCount(2);
        result.Select(m => m.Name).Should().Contain(new[] { "Alice", "Bob" });
    }

    [Fact]
    public async Task GetByIdAsync_ExistingMember_ReturnsMember()
    {
        var svc = CreateService(out var db);
        var member = new TeamMember { Name = "Alice" };
        db.TeamMembers.Add(member);
        await db.SaveChangesAsync();

        var result = await svc.GetByIdAsync(member.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var svc = CreateService(out _);

        var result = await svc.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_FirstMember_BecomesLeadAutomatically()
    {
        var svc = CreateService(out _);

        var result = await svc.CreateAsync(new CreateTeamMemberDto { Name = "Alice" });

        result.IsLead.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_SecondMember_IsNotLead()
    {
        var svc = CreateService(out var db);
        db.TeamMembers.Add(new TeamMember { Name = "Alice", IsLead = true });
        await db.SaveChangesAsync();

        var result = await svc.CreateAsync(new CreateTeamMemberDto { Name = "Bob" });

        result.IsLead.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_TrimsWhitespaceFromName()
    {
        var svc = CreateService(out _);

        var result = await svc.CreateAsync(new CreateTeamMemberDto { Name = "  Alice  " });

        result.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task UpdateAsync_ExistingMember_UpdatesName()
    {
        var svc = CreateService(out var db);
        var member = new TeamMember { Name = "Alice" };
        db.TeamMembers.Add(member);
        await db.SaveChangesAsync();

        var result = await svc.UpdateAsync(member.Id, new UpdateTeamMemberDto { Name = "Alicia" });

        result.Should().NotBeNull();
        result!.Name.Should().Be("Alicia");
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNull()
    {
        var svc = CreateService(out _);

        var result = await svc.UpdateAsync(Guid.NewGuid(), new UpdateTeamMemberDto { Name = "X" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task MakeLeadAsync_TransfersLeadFromCurrentLead()
    {
        var svc = CreateService(out var db);
        var alice = new TeamMember { Name = "Alice", IsLead = true };
        var bob = new TeamMember { Name = "Bob", IsLead = false };
        db.TeamMembers.AddRange(alice, bob);
        await db.SaveChangesAsync();

        var result = await svc.MakeLeadAsync(bob.Id);

        result.Should().BeTrue();
        db.TeamMembers.Find(alice.Id)!.IsLead.Should().BeFalse();
        db.TeamMembers.Find(bob.Id)!.IsLead.Should().BeTrue();
    }

    [Fact]
    public async Task MakeLeadAsync_InactiveMember_ReturnsFalse()
    {
        var svc = CreateService(out var db);
        var member = new TeamMember { Name = "Alice", IsActive = false };
        db.TeamMembers.Add(member);
        await db.SaveChangesAsync();

        var result = await svc.MakeLeadAsync(member.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateAsync_ActiveMember_DeactivatesAndRemovesLead()
    {
        var svc = CreateService(out var db);
        var member = new TeamMember { Name = "Alice", IsActive = true, IsLead = true };
        db.TeamMembers.Add(member);
        await db.SaveChangesAsync();

        var result = await svc.DeactivateAsync(member.Id);

        result.Should().BeTrue();
        var updated = db.TeamMembers.Find(member.Id)!;
        updated.IsActive.Should().BeFalse();
        updated.IsLead.Should().BeFalse();
    }

    [Fact]
    public async Task ReactivateAsync_InactiveMember_ReactivatesMember()
    {
        var svc = CreateService(out var db);
        var member = new TeamMember { Name = "Alice", IsActive = false };
        db.TeamMembers.Add(member);
        await db.SaveChangesAsync();

        var result = await svc.ReactivateAsync(member.Id);

        result.Should().BeTrue();
        db.TeamMembers.Find(member.Id)!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AnyExistsAsync_WhenMembersExist_ReturnsTrue()
    {
        var svc = CreateService(out var db);
        db.TeamMembers.Add(new TeamMember { Name = "Alice" });
        await db.SaveChangesAsync();

        var result = await svc.AnyExistsAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyExistsAsync_WhenEmpty_ReturnsFalse()
    {
        var svc = CreateService(out _);

        var result = await svc.AnyExistsAsync();

        result.Should().BeFalse();
    }
}
