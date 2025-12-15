using Microsoft.Extensions.Options;
using Sonic.Api.Config;
using Sonic.Application.Auth.interfaces;
using Sonic.Application.Users.interfaces;
using Sonic.Domain.Users;

namespace Sonic.Api.Bootstrap;

public sealed class AdminBootstrapHostedService(
    IServiceProvider serviceProvider,
    IOptions<AdminSeedOptions> options,
    ILogger<AdminBootstrapHostedService> logger) : IHostedService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly AdminSeedOptions _options = options.Value;
    private readonly ILogger<AdminBootstrapHostedService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Admin seed disabled (AdminSeed.Enabled=false).");
            return;
        }

        var email = (_options.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = _options.Password ?? string.Empty;
        var displayName = string.IsNullOrWhiteSpace(_options.DisplayName) ? "Sonic Admin" : _options.DisplayName.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("AdminSeed.Email is required when AdminSeed.Enabled=true.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new InvalidOperationException("AdminSeed.Password is required (min 8 chars) when AdminSeed.Enabled=true.");
        }

        using var scope = _serviceProvider.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var existing = await userRepo.GetByEmailAsync(email, cancellationToken);

        if (existing is not null)
        {
            if (existing.Role != UserRole.Admin)
            {
                throw new InvalidOperationException(
                    $"Admin seed email exists but role is not Admin. Email='{email}'. Change AdminSeed.Email or fix manually.");
            }

            if (_options.ResetPasswordOnStartup)
            {
                existing.SetPasswordHash(hasher.Hash(password)); // add this domain method if missing
                existing.UpdateProfile(existing.DisplayName, existing.Bio, existing.JobRole, existing.Interests, existing.AvatarUrl); // just to bump UpdatedAt if you want
                await userRepo.UpdateAsync(existing, cancellationToken);

                _logger.LogWarning("Admin password reset on startup. Email='{Email}'.", email);
            }
            else
            {
                _logger.LogInformation("Admin already exists. Email='{Email}'.", email);
            }

            return;
        }

        var passwordHash = hasher.Hash(password);

        var admin = User.CreateNew(
            email: email,
            passwordHash: passwordHash,
            displayName: displayName,
            role: UserRole.Admin);

        await userRepo.InsertAsync(admin, cancellationToken);

        _logger.LogInformation("Admin seeded successfully. Email='{Email}'.", email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
