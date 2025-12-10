using Sonic.Domain.Users;

namespace Sonic.Infrastructure.Persistence.Documents;

internal sealed class UserDocument
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string NormalizedEmail { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? Bio { get; set; }
    public string? JobRole { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> Interests { get; set; } = new();
    public string Role { get; set; } = UserRole.User.ToString();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
