using Sonic.Application.Users.interfaces;
using Sonic.Domain.Users;

namespace Sonic.Tests.Fakes;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<string, User> _byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _idByEmail = new(StringComparer.OrdinalIgnoreCase);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult<User?>(null);
        }

        if (_idByEmail.TryGetValue(email.Trim(), out var id) && _byId.TryGetValue(id, out var user))
        {
            return Task.FromResult<User?>(user);
        }

        return Task.FromResult<User?>(null);
    }

    public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult<User?>(null);
        }

        _byId.TryGetValue(id, out var user);
        return Task.FromResult<User?>(user);
    }

    public Task InsertAsync(User user, CancellationToken cancellationToken = default)
    {
        _byId[user.Id] = user;
        _idByEmail[user.Email] = user.Id;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _byId[user.Id] = user;
        _idByEmail[user.Email] = user.Id;
        return Task.CompletedTask;
    }
}
