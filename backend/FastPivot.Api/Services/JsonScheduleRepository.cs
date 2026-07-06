using System.Text.Json;
using FastPivot.Api.Models;

namespace FastPivot.Api.Services;

public interface IScheduleRepository
{
    Task<IReadOnlyList<TestScheduleItem>> GetSchedulesAsync(CancellationToken cancellationToken);
}

public sealed class JsonScheduleRepository : IScheduleRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public JsonScheduleRepository(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<TestScheduleItem>> GetSchedulesAsync(CancellationToken cancellationToken)
    {
        var configuredPath = _configuration["Schedules:JsonPath"];
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(_environment.ContentRootPath, "Data", "sample-schedules.json")
            : configuredPath;

        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(_environment.ContentRootPath, path);
        }

        await using var stream = File.OpenRead(path);
        var schedules = await JsonSerializer.DeserializeAsync<List<TestScheduleItem>>(
            stream,
            JsonOptions,
            cancellationToken);

        return schedules ?? [];
    }
}
