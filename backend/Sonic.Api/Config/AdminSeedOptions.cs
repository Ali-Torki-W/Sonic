namespace Sonic.Api.Config;

public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public bool Enabled { get; init; }

    // DEV ONLY convenience. Keep false in prod.
    public bool ResetPasswordOnStartup { get; init; }

    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DisplayName { get; init; } = "Sonic Admin";
}
