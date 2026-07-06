using FastPivot.Api.Authorization;
using FastPivot.Api.Models;
using FastPivot.Api.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;

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
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services
    .AddOptions<WindowsAuthorizationOptions>()
    .Bind(builder.Configuration.GetSection(WindowsAuthorizationOptions.SectionName))
    .Validate(options => options.AllowedGroups.All(group => !string.IsNullOrWhiteSpace(group)), "Allowed groups cannot be empty.")
    .ValidateOnStart();

builder.Services
    .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("WindowsGroupAccess", policy =>
    {
        policy.AddAuthenticationSchemes(NegotiateDefaults.AuthenticationScheme);
        policy.AddRequirements(new WindowsGroupRequirement());
    });
});

builder.Services.AddSingleton<IAuthorizationHandler, WindowsGroupAuthorizationHandler>();
builder.Services.AddSingleton<IScheduleRepository, JsonScheduleRepository>();
builder.Services.AddSingleton<ISchedulePivotService, SchedulePivotService>();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api")
    .RequireAuthorization("WindowsGroupAccess");

api.MapGet("/schedules", async (
    IScheduleRepository repository,
    CancellationToken cancellationToken) =>
{
    var schedules = await repository.GetSchedulesAsync(cancellationToken);
    return Results.Ok(schedules);
});

api.MapGet("/pivot/current-week", async (
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

api.MapGet("/pivot", async (
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

api.MapGet("/pivot/cell-key", (DateOnly date, string testSuite) =>
{
    return Results.Ok(new { key = SchedulePivotService.CreateCellKey(date, testSuite) });
});

app.Run();
