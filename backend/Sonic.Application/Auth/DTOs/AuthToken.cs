namespace Sonic.Application.Auth.DTOs;

public sealed record AuthToken(
    string AccessToken,
    DateTime ExpiresAtUtc
);