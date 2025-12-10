using MongoDB.Driver;
using Sonic.Application.Users.interfaces;
using Sonic.Domain.Users;
using Sonic.Infrastructure.Config;
using Sonic.Infrastructure.Persistence.Documents;

namespace Sonic.Infrastructure.Users;

public sealed class UserRepository(MongoDbContext dbContext) : IUserRepository
{
    private readonly IMongoCollection<UserDocument> _collection = dbContext.GetCollection<UserDocument>("users");

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = email.Trim().ToLowerInvariant();

        var filter = Builders<UserDocument>.Filter.Eq(x => x.Email, normalizedEmail);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        return doc is null ? null : ToDomain(doc);
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, id);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        return doc is null ? null : ToDomain(doc);
    }

    public async Task InsertAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));

        var doc = FromDomain(user);
        await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));

        var doc = FromDomain(user);
        var filter = Builders<UserDocument>.Filter.Eq(x => x.Id, user.Id);
        await _collection.ReplaceOneAsync(filter, doc, cancellationToken: cancellationToken);
    }

    // ---- Mapping ----

    private static User ToDomain(UserDocument doc)
    {
        var role = Enum.TryParse<UserRole>(doc.Role, ignoreCase: true, out var parsedRole)
            ? parsedRole
            : UserRole.User;

        return User.FromPersistence(
            id: doc.Id,
            email: doc.Email,
            passwordHash: doc.PasswordHash,
            displayName: doc.DisplayName,
            role: role,
            createdAt: doc.CreatedAt,
            updatedAt: doc.UpdatedAt,
            interests: doc.Interests ?? Enumerable.Empty<string>(),
            bio: doc.Bio,
            jobRole: doc.JobRole,
            avatarUrl: doc.AvatarUrl);
    }

    private static UserDocument FromDomain(User user)
    {
        return new UserDocument
        {
            Id = user.Id,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            JobRole = user.JobRole,
            Interests = user.Interests.ToList(),
            AvatarUrl = user.AvatarUrl,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
