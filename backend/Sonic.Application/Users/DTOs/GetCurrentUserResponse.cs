namespace Sonic.Application.Users.DTOs;

public sealed class GetCurrentUserResponse
{
    public string Id { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string? Bio { get; init; }
    public string? JobRole { get; init; }
    public List<string> Interests { get; init; } = new();
    public string? AvatarUrl { get; init; }
    public string Role { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
