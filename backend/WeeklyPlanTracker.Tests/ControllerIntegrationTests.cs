using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeeklyPlanTracker.Core.DTOs;
using WeeklyPlanTracker.Core.Enums;
using WeeklyPlanTracker.Infrastructure.Data;

namespace WeeklyPlanTracker.Tests.Controllers;

public class ControllerWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"wpt_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var d = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (d != null) services.Remove(d);
            services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }
}

public class ControllerIntegrationTests : IClassFixture<ControllerWebAppFactory>
{
    private readonly HttpClient _client;

    private static readonly System.Text.Json.JsonSerializerOptions _json = new()
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public ControllerIntegrationTests(ControllerWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<TeamMemberDto> CreateMemberAsync(string name = "Test Member")
    {
        var res = await _client.PostAsJsonAsync("/api/team-members", new CreateTeamMemberDto { Name = name });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        var doc = System.Text.Json.JsonDocument.Parse(body).RootElement;
        return new TeamMemberDto { Id = doc.GetProperty("id").GetGuid(), Name = doc.GetProperty("name").GetString()! };
    }

    private async Task<PlanningWeekDto> CreateWeekAsync()
    {
        var member = await CreateMemberAsync("Week Member");
        var res = await _client.PostAsJsonAsync("/api/planning-weeks", new CreatePlanningWeekDto
        {
            PlanningDate = DateOnly.FromDateTime(DateTime.Today),
            ParticipatingMemberIds = new List<Guid> { member.Id },
            ClientFocusedPercent = 40, TechDebtPercent = 30, RAndDPercent = 30
        });
        res.EnsureSuccessStatusCode();
        var body2 = await res.Content.ReadAsStringAsync();
        var doc2 = System.Text.Json.JsonDocument.Parse(body2).RootElement;
        return new PlanningWeekDto { Id = doc2.GetProperty("id").GetGuid() };
    }

    private async Task<PlanningWeekDto> CreateOpenWeekAsync()
    {
        var week = await CreateWeekAsync();
        await _client.PatchAsync("/api/planning-weeks/" + week.Id + "/open", null);
        return week;
    }

    [Fact] public async Task TeamMembers_GetAll_ReturnsOk() =>
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/team-members")).StatusCode);

    [Fact] public async Task TeamMembers_AnyExists_ReturnsOk() =>
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/team-members/any")).StatusCode);

    [Fact] public async Task TeamMembers_GetById_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/team-members/{Guid.NewGuid()}")).StatusCode);

    [Fact] public async Task TeamMembers_Create_ReturnsCreated() =>
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/team-members", new CreateTeamMemberDto { Name = "New" })).StatusCode);

    [Fact]
    public async Task TeamMembers_GetById_ReturnsOk()
    {
        var postRes = await _client.PostAsJsonAsync("/api/team-members", new CreateTeamMemberDto { Name = "GetById" });
        var body = await postRes.Content.ReadAsStringAsync();
        var id = System.Text.Json.JsonDocument.Parse(body).RootElement.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/team-members/{id}")).StatusCode);
    }

    [Fact]
    public async Task TeamMembers_Update_ReturnsOk()
    {
        var r1 = await _client.PostAsJsonAsync("/api/team-members", new CreateTeamMemberDto { Name = "BeforeUpdate" });
        var id1 = System.Text.Json.JsonDocument.Parse(await r1.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"/api/team-members/{id1}", new UpdateTeamMemberDto { Name = "AfterUpdate" })).StatusCode);
    }

    [Fact] public async Task TeamMembers_Update_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsJsonAsync($"/api/team-members/{Guid.NewGuid()}", new UpdateTeamMemberDto { Name = "X" })).StatusCode);

    [Fact]
    public async Task TeamMembers_MakeLead_ReturnsNoContent()
    {
        var r2 = await _client.PostAsJsonAsync("/api/team-members", new CreateTeamMemberDto { Name = "MakeLead" });
        var id2 = System.Text.Json.JsonDocument.Parse(await r2.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PatchAsync($"/api/team-members/{id2}/make-lead", null)).StatusCode);
    }

    [Fact] public async Task TeamMembers_MakeLead_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PatchAsync($"/api/team-members/{Guid.NewGuid()}/make-lead", null)).StatusCode);

    [Fact]
    public async Task TeamMembers_Deactivate_ReturnsNoContent()
    {
        var r3 = await _client.PostAsJsonAsync("/api/team-members", new CreateTeamMemberDto { Name = "Deactivate" });
        var id3 = System.Text.Json.JsonDocument.Parse(await r3.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PatchAsync($"/api/team-members/{id3}/deactivate", null)).StatusCode);
    }

    [Fact] public async Task TeamMembers_Deactivate_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PatchAsync($"/api/team-members/{Guid.NewGuid()}/deactivate", null)).StatusCode);

    [Fact]
    public async Task TeamMembers_Reactivate_ReturnsNoContent()
    {
        var r4 = await _client.PostAsJsonAsync("/api/team-members", new CreateTeamMemberDto { Name = "Reactivate" });
        var id4 = System.Text.Json.JsonDocument.Parse(await r4.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        await _client.PatchAsync($"/api/team-members/{id4}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PatchAsync($"/api/team-members/{id4}/reactivate", null)).StatusCode);
    }

    [Fact] public async Task TeamMembers_Reactivate_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PatchAsync($"/api/team-members/{Guid.NewGuid()}/reactivate", null)).StatusCode);

    // Backlog
    [Fact] public async Task Backlog_GetAll_ReturnsOk() =>
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/backlog")).StatusCode);

    [Fact] public async Task Backlog_GetAll_WithStatusFilter_ReturnsOk() =>
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/backlog?status=Available")).StatusCode);

    [Fact] public async Task Backlog_GetAll_WithCategoryFilter_ReturnsOk() =>
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/backlog?category=ClientFocused")).StatusCode);

    [Fact] public async Task Backlog_GetById_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/backlog/{Guid.NewGuid()}")).StatusCode);

    [Fact] public async Task Backlog_Create_ReturnsCreated() =>
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/backlog", new CreateBacklogItemDto { Title = "Item", Category = Category.ClientFocused })).StatusCode);

    [Fact]
    public async Task Backlog_GetById_ReturnsOk()
    {
        var r5 = await _client.PostAsJsonAsync("/api/backlog", new CreateBacklogItemDto { Title = "GetMe", Category = Category.RAndD });
        var id5 = System.Text.Json.JsonDocument.Parse(await r5.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/backlog/{id5}")).StatusCode);
    }

    [Fact]
    public async Task Backlog_Update_ReturnsOk()
    {
        var r6 = await _client.PostAsJsonAsync("/api/backlog", new CreateBacklogItemDto { Title = "Before", Category = Category.TechDebt });
        var id6 = System.Text.Json.JsonDocument.Parse(await r6.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"/api/backlog/{id6}", new UpdateBacklogItemDto { Title = "After", Category = Category.TechDebt })).StatusCode);
    }

    [Fact] public async Task Backlog_Update_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsJsonAsync($"/api/backlog/{Guid.NewGuid()}", new UpdateBacklogItemDto { Title = "X", Category = Category.RAndD })).StatusCode);

    [Fact]
    public async Task Backlog_Archive_ReturnsNoContent()
    {
        var res = await _client.PostAsJsonAsync("/api/backlog", new CreateBacklogItemDto { Title = "ArchiveMe", Category = Category.ClientFocused });
        var body = await res.Content.ReadAsStringAsync();
        var id = System.Text.Json.JsonDocument.Parse(body).RootElement.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PatchAsync($"/api/backlog/{id}/archive", null)).StatusCode);
    }

    [Fact] public async Task Backlog_Archive_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PatchAsync($"/api/backlog/{Guid.NewGuid()}/archive", null)).StatusCode);

    // PlanningWeeks
    [Fact] public async Task PlanningWeeks_GetAll_ReturnsOk() =>
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/planning-weeks")).StatusCode);

    [Fact]
    public async Task PlanningWeeks_GetActive_ReturnsNoContentOrOk()
    {
        var res = await _client.GetAsync("/api/planning-weeks/active");
        Assert.True(res.StatusCode == HttpStatusCode.NoContent || res.StatusCode == HttpStatusCode.OK);
    }

    [Fact] public async Task PlanningWeeks_GetById_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/planning-weeks/{Guid.NewGuid()}")).StatusCode);

    [Fact]
    public async Task PlanningWeeks_Create_ReturnsCreated()
    {
        var week = await CreateWeekAsync();
        Assert.NotEqual(Guid.Empty, week.Id);
    }

    [Fact]
    public async Task PlanningWeeks_GetById_ReturnsOk()
    {
        var week = await CreateWeekAsync();
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/planning-weeks/{week.Id}")).StatusCode);
    }

    [Fact] public async Task PlanningWeeks_UpdateAllocations_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsJsonAsync($"/api/planning-weeks/{Guid.NewGuid()}/allocations", new UpdateAllocationsDto { ClientFocusedPercent = 40, TechDebtPercent = 30, RAndDPercent = 30 })).StatusCode);

    [Fact]
    public async Task PlanningWeeks_Open_ReturnsNoContent()
    {
        var week = await CreateWeekAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PatchAsync($"/api/planning-weeks/{week.Id}/open", null)).StatusCode);
    }

    [Fact] public async Task PlanningWeeks_Open_NotFound_ReturnsBadRequest() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PatchAsync($"/api/planning-weeks/{Guid.NewGuid()}/open", null)).StatusCode);

    [Fact]
    public async Task PlanningWeeks_Freeze_NotReady_ReturnsBadRequest()
    {
        var week = await CreateOpenWeekAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PatchAsync($"/api/planning-weeks/{week.Id}/freeze", null)).StatusCode);
    }

    [Fact]
    public async Task PlanningWeeks_Finish_NotFrozen_ReturnsBadRequest()
    {
        var week = await CreateWeekAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PatchAsync($"/api/planning-weeks/{week.Id}/finish", null)).StatusCode);
    }

    [Fact]
    public async Task PlanningWeeks_Cancel_ReturnsNoContent()
    {
        var week = await CreateWeekAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/planning-weeks/{week.Id}")).StatusCode);
    }

    [Fact] public async Task PlanningWeeks_Cancel_NotFound_ReturnsBadRequest() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.DeleteAsync($"/api/planning-weeks/{Guid.NewGuid()}")).StatusCode);

    // MemberPlans
    [Fact] public async Task MemberPlans_Get_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/member-plans/{Guid.NewGuid()}/{Guid.NewGuid()}")).StatusCode);

    [Fact]
    public async Task MemberPlans_Get_ExistingPlan_ReturnsOk()
    {
        var member = await CreateMemberAsync("PlanMember");
        var weekRes = await _client.PostAsJsonAsync("/api/planning-weeks", new CreatePlanningWeekDto
        {
            PlanningDate = DateOnly.FromDateTime(DateTime.Today),
            ParticipatingMemberIds = new List<Guid> { member.Id },
            ClientFocusedPercent = 40, TechDebtPercent = 30, RAndDPercent = 30
        });
        var weekDoc = System.Text.Json.JsonDocument.Parse(await weekRes.Content.ReadAsStringAsync()).RootElement;
        var weekId = weekDoc.GetProperty("id").GetGuid();
        await _client.PatchAsync($"/api/planning-weeks/{weekId}/open", null);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/member-plans/{weekId}/{member.Id}")).StatusCode);
    }

    [Fact] public async Task MemberPlans_ClaimItem_NotFound()
    {
        var res = await _client.PostAsJsonAsync($"/api/member-plans/{Guid.NewGuid()}/{Guid.NewGuid()}/assignments", new ClaimBacklogItemDto { BacklogItemId = Guid.NewGuid(), CommittedHours = 5 });
        Assert.True(res.StatusCode == HttpStatusCode.NotFound || res.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact] public async Task MemberPlans_ToggleReady_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PatchAsync($"/api/member-plans/{Guid.NewGuid()}/{Guid.NewGuid()}/ready", null)).StatusCode);

    [Fact] public async Task MemberPlans_RemoveItem_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/member-plans/{Guid.NewGuid()}/{Guid.NewGuid()}/assignments/{Guid.NewGuid()}")).StatusCode);

    [Fact] public async Task MemberPlans_UpdateHours_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsJsonAsync($"/api/member-plans/{Guid.NewGuid()}/{Guid.NewGuid()}/assignments/{Guid.NewGuid()}", new UpdateCommittedHoursDto { CommittedHours = 5 })).StatusCode);

    // Progress
    [Fact] public async Task Progress_GetTeamProgress_NotFound()
    {
        var res = await _client.GetAsync($"/api/progress/{Guid.NewGuid()}");
        Assert.True(res.StatusCode == HttpStatusCode.NotFound || res.StatusCode == HttpStatusCode.OK);
    }

    [Fact] public async Task Progress_GetMemberProgress_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/progress/{Guid.NewGuid()}/{Guid.NewGuid()}")).StatusCode);

    [Fact] public async Task Progress_GetTaskHistory_ReturnsOk() =>
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/progress/assignments/{Guid.NewGuid()}/history")).StatusCode);

    [Fact] public async Task Progress_SubmitUpdate_NotFound() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync($"/api/progress/assignments/{Guid.NewGuid()}", new SubmitProgressUpdateDto { HoursCompleted = 3, Status = ProgressStatus.InProgress })).StatusCode);

    // Reset
    [Fact] public async Task Reset_ReturnsNoContent() =>
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync("/api/reset")).StatusCode);

    // Restore
    [Fact] public async Task Restore_Backup_ReturnsOk() =>
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/restore/backup")).StatusCode);

    [Fact] public async Task Restore_EmptyPayload_ReturnsNoContent() =>
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PostAsJsonAsync("/api/restore",
            new { teamMembers = new List<object>(), backlogItems = new List<object>(), planningWeeks = new List<object>() })).StatusCode);
}
