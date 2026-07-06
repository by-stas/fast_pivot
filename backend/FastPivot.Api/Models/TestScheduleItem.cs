namespace FastPivot.Api.Models;

public sealed record TestScheduleItem
{
    public required string Project { get; init; }
    public required string Title { get; init; }
    public required string Crontab { get; init; }
    public required string Description { get; init; }
    public required string Product { get; init; }
    public required string Version { get; init; }
    public required string Machine { get; init; }
    public required string Image { get; init; }
    public required string TestSuite { get; init; }
    public required string Test { get; init; }
}
