namespace Sonic.Application.Auth;

public sealed record AuthToken(
    string AccessToken,
    DateTime ExpiresAtUtc
);