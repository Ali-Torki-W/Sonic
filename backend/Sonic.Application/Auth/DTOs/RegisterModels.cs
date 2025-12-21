namespace Sonic.Application.Auth.DTOs;

using System.ComponentModel.DataAnnotations;

public sealed class RegisterRequest
{
    private const string EmailRegex = @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$";

    // Password: 8-64 chars, at least 1 lower, 1 upper, 1 digit
    // (Add special char requirement if you want: (?=.*[^A-Za-z0-9]))
    private const string PasswordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,64}$";

    [Required]
    [StringLength(254, MinimumLength = 3)]
    [RegularExpression(EmailRegex, ErrorMessage = "Email is invalid (example: name@email.com).")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 8)]
    [RegularExpression(PasswordRegex, ErrorMessage = "Password must be 8-64 chars and include uppercase, lowercase, and a number.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;
}


public sealed class RegisterResponse
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
}
