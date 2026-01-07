namespace Sonic.Application.Users.DTOs;

public sealed class PublicProfileResponse
{
    public string Id { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string? Bio { get; init; }
    public string? JobRole { get; set; }
    public string? AvatarUrl { get; init; }
}
