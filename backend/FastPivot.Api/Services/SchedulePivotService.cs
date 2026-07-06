using FastPivot.Api.Models;

namespace FastPivot.Api.Services;

public interface ISchedulePivotService
{
    Task<PivotResponse> BuildPivotAsync(DateOnly weekStart, string timeZoneId, CancellationToken cancellationToken);
}

public sealed class SchedulePivotService : ISchedulePivotService
{
    private readonly IScheduleRepository _repository;

    public SchedulePivotService(IScheduleRepository repository)
    {
        _repository = repository;
    }

    public async Task<PivotResponse> BuildPivotAsync(DateOnly weekStart, string timeZoneId, CancellationToken cancellationToken)
    {
        var timeZone = ResolveTimeZone(timeZoneId);
        var normalizedWeekStart = StartOfWeek(weekStart);
        var weekEnd = normalizedWeekStart.AddDays(6);
        var schedules = await _repository.GetSchedulesAsync(cancellationToken);
        var rows = new Dictionary<RowKey, Dictionary<string, List<PivotCellItem>>>();
        var testSuites = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<PivotWarning>();

        foreach (var schedule in schedules)
        {
            testSuites.Add(schedule.TestSuite);

            CronSchedule cronSchedule;
            try
            {
                cronSchedule = CronSchedule.Parse(schedule.Crontab);
            }
            catch (FormatException exception)
            {
                warnings.Add(new PivotWarning(schedule.Title, schedule.Crontab, exception.Message));
                continue;
            }

            var occurrences = cronSchedule.GetOccurrences(normalizedWeekStart, weekEnd, timeZone);
            foreach (var occurrence in occurrences)
            {
                var rowKey = new RowKey(schedule.Project, schedule.Product, schedule.Version);
                var cellKey = CreateCellKey(DateOnly.FromDateTime(occurrence.DateTime), schedule.TestSuite);

                if (!rows.TryGetValue(rowKey, out var rowCells))
                {
                    rowCells = new Dictionary<string, List<PivotCellItem>>(StringComparer.Ordinal);
                    rows[rowKey] = rowCells;
                }

                if (!rowCells.TryGetValue(cellKey, out var cellItems))
                {
                    cellItems = [];
                    rowCells[cellKey] = cellItems;
                }

                cellItems.Add(new PivotCellItem(
                    schedule.Title,
                    schedule.Description,
                    schedule.Machine,
                    schedule.Image,
                    schedule.Test,
                    schedule.TestSuite,
                    TimeOnly.FromDateTime(occurrence.DateTime)));
            }
        }

        var days = Enumerable.Range(0, 7)
            .Select(offset => normalizedWeekStart.AddDays(offset))
            .Select(date => new PivotDay(date, date.ToString("ddd MM/dd")))
            .ToArray();

        var pivotRows = rows
            .OrderBy(row => row.Key.Project, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Key.Product, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Key.Version, StringComparer.OrdinalIgnoreCase)
            .Select(row => new PivotRow(
                row.Key.Project,
                row.Key.Product,
                row.Key.Version,
                row.Value.ToDictionary(
                    cell => cell.Key,
                    cell => (IReadOnlyList<PivotCellItem>)cell.Value
                        .OrderBy(item => item.ScheduledTime)
                        .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    StringComparer.Ordinal)))
            .ToArray();

        return new PivotResponse(
            normalizedWeekStart,
            weekEnd,
            days,
            testSuites.ToArray(),
            pivotRows,
            warnings);
    }

    public static DateOnly StartOfWeek(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    public static string CreateCellKey(DateOnly date, string testSuite)
    {
        return $"{date:yyyy-MM-dd}|{testSuite}";
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new ArgumentException($"Unknown timezone '{timeZoneId}'.", nameof(timeZoneId), exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new ArgumentException($"Invalid timezone '{timeZoneId}'.", nameof(timeZoneId), exception);
        }
    }

    private sealed record RowKey(string Project, string Product, string Version);
}
