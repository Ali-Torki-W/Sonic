namespace Sonic.Application.Users.DTOs;

public sealed class UpdateProfileRequest
{
    public string DisplayName { get; init; } = default!;
    public string? Bio { get; init; }
    public string? JobRole { get; init; }
    public List<string>? Interests { get; init; }
    public string? AvatarUrl { get; init; }
}
