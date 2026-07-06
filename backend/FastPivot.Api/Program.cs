using FastPivot.Api.Models;
using FastPivot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IScheduleRepository, JsonScheduleRepository>();
builder.Services.AddSingleton<ISchedulePivotService, SchedulePivotService>();

var app = builder.Build();

app.UseCors();

app.MapGet("/api/schedules", async (
    IScheduleRepository repository,
    CancellationToken cancellationToken) =>
{
    var schedules = await repository.GetSchedulesAsync(cancellationToken);
    return Results.Ok(schedules);
});

app.MapGet("/api/pivot/current-week", async (
    ISchedulePivotService pivotService,
    string? timezone,
    CancellationToken cancellationToken) =>
{
    try
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var response = await pivotService.BuildPivotAsync(today, timezone ?? "UTC", cancellationToken);
        return Results.Ok(response);
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

app.MapGet("/api/pivot", async (
    ISchedulePivotService pivotService,
    DateOnly weekStart,
    string? timezone,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await pivotService.BuildPivotAsync(weekStart, timezone ?? "UTC", cancellationToken);
        return Results.Ok(response);
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

app.MapGet("/api/pivot/cell-key", (DateOnly date, string testSuite) =>
{
    return Results.Ok(new { key = SchedulePivotService.CreateCellKey(date, testSuite) });
});

app.Run();
