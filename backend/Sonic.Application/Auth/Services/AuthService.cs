using Sonic.Application.Auth.DTOs;
using Sonic.Application.Auth.interfaces;
using Sonic.Application.Common.Errors;
using Sonic.Application.Users;
using Sonic.Domain.Users;

namespace Sonic.Application.Auth.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator = jwtTokenGenerator;

    public async Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.Email))
            throw Errors.BadRequest("Email is required.", "auth.email_required");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw Errors.BadRequest("Password is required.", "auth.password_required");

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            throw Errors.BadRequest("Display name is required.", "auth.displayname_required");

        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            // This is the duplicate email case → 409, not 500
            throw Errors.Conflict("Email is already in use.", "auth.email_in_use");
        }

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = User.CreateNew(
            email: request.Email,
            passwordHash: passwordHash,
            displayName: request.DisplayName,
            role: UserRole.User);

        await _userRepository.InsertAsync(user, cancellationToken);

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString(),
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc
        };
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw Errors.BadRequest("Email and password are required.", "auth.missing_credentials");
        }

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            // Do not leak if email exists or not
            throw Errors.Unauthorized("Invalid email or password.", "auth.invalid_credentials");
        }

        var passwordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            throw Errors.Unauthorized("Invalid email or password.", "auth.invalid_credentials");
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString(),
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc
        };
    }
}
