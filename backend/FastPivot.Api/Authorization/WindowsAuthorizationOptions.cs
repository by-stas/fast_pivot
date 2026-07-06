namespace FastPivot.Api.Authorization;

public sealed class WindowsAuthorizationOptions
{
    public const string SectionName = "WindowsAuthorization";

    public string[] AllowedGroups { get; set; } = ["Everyone"];
}
