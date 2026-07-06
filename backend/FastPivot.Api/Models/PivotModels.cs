namespace FastPivot.Api.Models;

public sealed record PivotResponse(
    DateOnly WeekStart,
    DateOnly WeekEnd,
    IReadOnlyList<PivotDay> Days,
    IReadOnlyList<string> TestSuites,
    IReadOnlyList<PivotRow> Rows,
    IReadOnlyList<PivotWarning> Warnings);

public sealed record PivotDay(DateOnly Date, string Label);

public sealed record PivotRow(
    string Project,
    string Product,
    string Version,
    IReadOnlyDictionary<string, IReadOnlyList<PivotCellItem>> Cells);

public sealed record PivotCellItem(
    string Title,
    string Description,
    string Machine,
    string Image,
    string Test,
    string TestSuite,
    TimeOnly ScheduledTime);

public sealed record PivotWarning(string Title, string Crontab, string Message);
