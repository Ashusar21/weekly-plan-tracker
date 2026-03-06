using Microsoft.EntityFrameworkCore;
using WeeklyPlanTracker.Core.Interfaces;
using WeeklyPlanTracker.Infrastructure.Data;
using WeeklyPlanTracker.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString != null && connectionString.Contains("neon.tech"))
    builder.Services.AddDbContext<AppDbContext>(opts =>
        opts.UseNpgsql(connectionString));
else
    builder.Services.AddDbContext<AppDbContext>(opts =>
        opts.UseSqlite(connectionString ?? "Data Source=weeklyplanner.db"));

builder.Services.AddScoped<ITeamMemberService, TeamMemberService>();
builder.Services.AddScoped<IBacklogService, BacklogService>();
builder.Services.AddScoped<IPlanningWeekService, PlanningWeekService>();
builder.Services.AddScoped<IMemberPlanService, MemberPlanService>();
builder.Services.AddScoped<IProgressService, ProgressService>();

builder.Services.AddCors(options =>
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200", "https://brave-coast-086bb810f.6.azurestaticapps.net")
              .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.EnvironmentName == "Testing")
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();
}

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }