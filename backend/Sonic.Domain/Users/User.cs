using System;
using System.Collections.Generic;
using System.Linq;
using Sonic.Domain.Users;

namespace Sonic.Domain.Users;

/// <summary>
/// Core user entity for Sonic.
///
/// Invariants / rules:
/// - Id: string-based, unique per user (generated as Guid string in CreateNew).
/// - Email: required, trimmed, lowercased, simple format-checked.
/// - PasswordHash: required, non-empty (actual hashing done in Application layer).
/// - DisplayName: required, trimmed, max 100 chars.
/// - Bio: optional, max 1000 chars.
/// - JobRole: optional, max 200 chars.
/// - Interests: optional list of tags; each trimmed, <= 100 chars, distinct (case-insensitive).
/// - AvatarUrl: optional, must be a valid absolute URL if set.
/// - Role: User or Admin; defaults to User in normal registration.
/// - CreatedAt / UpdatedAt: UTC timestamps (UTC only).
/// </summary>
public sealed class User
{
    // We keep Id as string so Domain is storage-agnostic.
    // Infrastructure (Mongo) will map this to ObjectId or any other persistence type.
    public string Id { get; private set; } = default!;

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;

    public string? Bio { get; private set; }
    public string? JobRole { get; private set; }

    private readonly List<string> _interests = new();
    public IReadOnlyCollection<string> Interests => _interests.AsReadOnly();

    public string? AvatarUrl { get; private set; }

    public UserRole Role { get; private set; }

    /// <summary>
    /// UTC timestamps. Always store and reason in UTC.
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // For serializers / ORMs / Mongo driver.
    // Normal code should not call this directly.
    private User()
    {
    }

    private User(
        string id,
        string email,
        string passwordHash,
        string displayName,
        UserRole role,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<string>? interests = null,
        string? bio = null,
        string? jobRole = null,
        string? avatarUrl = null)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        Role = role;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Bio = bio;
        JobRole = jobRole;
        AvatarUrl = avatarUrl;

        if (interests is not null)
        {
            _interests.AddRange(NormalizeInterests(interests));
        }

        Validate();
    }

    public static User FromPersistence(
    string id,
    string email,
    string passwordHash,
    string displayName,
    UserRole role,
    DateTime createdAt,
    DateTime updatedAt,
    IEnumerable<string>? interests = null,
    string? bio = null,
    string? jobRole = null,
    string? avatarUrl = null)
    {
        return new User(
            id: id,
            email: email,
            passwordHash: passwordHash,
            displayName: displayName,
            role: role,
            createdAt: createdAt,
            updatedAt: updatedAt,
            interests: interests,
            bio: bio,
            jobRole: jobRole,
            avatarUrl: avatarUrl);
    }

    /// <summary>
    /// Factory for a brand new user created via normal registration flow.
    /// Role defaults to User. Admin creation is handled via seeding/bootstrap or
    /// by explicitly passing UserRole.Admin from trusted code.
    /// </summary>
    public static User CreateNew(
        string email,
        string passwordHash,
        string displayName,
        UserRole role = UserRole.User)
    {
        if (role != UserRole.User && role != UserRole.Admin)
        {
            throw new ArgumentOutOfRangeException(nameof(role), "Invalid user role.");
        }

        var now = DateTime.UtcNow;
        var id = Guid.NewGuid().ToString("N"); // compact 32-char hex

        return new User(
            id: id,
            email: NormalizeEmail(email),
            passwordHash: NormalizePasswordHash(passwordHash),
            displayName: NormalizeDisplayName(displayName),
            role: role,
            createdAt: now,
            updatedAt: now);
    }

    /// <summary>
    /// Update profile fields that are editable by the user.
    /// Email and password are not changed here.
    /// </summary>
    public void UpdateProfile(
        string displayName,
        string? bio,
        string? jobRole,
        IEnumerable<string>? interests,
        string? avatarUrl)
    {
        DisplayName = NormalizeDisplayName(displayName);
        Bio = NormalizeOptionalText(bio, maxLength: 1000);
        JobRole = NormalizeOptionalText(jobRole, maxLength: 200);
        AvatarUrl = NormalizeOptionalUrl(avatarUrl);

        _interests.Clear();
        if (interests is not null)
        {
            _interests.AddRange(NormalizeInterests(interests));
        }

        UpdatedAt = DateTime.UtcNow;

        ValidateProfile();
    }

    /// <summary>
    /// Set or change the stored password hash (after registration or password change).
    /// The actual hashing is handled by a dedicated service in the Application layer.
    /// </summary>
    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = NormalizePasswordHash(passwordHash);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Promote a user to admin. To be used by admin/bootstrapping logic only. (we'll seed our first admin)
    /// </summary>
    public void PromoteToAdmin()
    {
        if (Role == UserRole.Admin)
        {
            return;
        }

        Role = UserRole.Admin;
        UpdatedAt = DateTime.UtcNow;
    }

    // ---- Internal helpers & validation ----

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        email = email.Trim();

        // Basic sanity check; not full RFC validation, good enough for MVP.
        if (!email.Contains('@') || email.Length > 320)
        {
            throw new ArgumentException("Email format looks invalid.", nameof(email));
        }

        return email.ToLowerInvariant();
    }

    private static string NormalizePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        return passwordHash.Trim();
    }

    private static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        displayName = displayName.Trim();

        if (displayName.Length > 100)
        {
            throw new ArgumentException("Display name is too long (max 100 characters).", nameof(displayName));
        }

        return displayName;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Text is too long (max {maxLength} characters).");
        }

        return normalized;
    }

    private static string? NormalizeOptionalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var trimmed = url.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Avatar URL is not a valid absolute URL.", nameof(url));
        }

        return trimmed;
    }

    private static IEnumerable<string> NormalizeInterests(IEnumerable<string> interests)
    {
        return interests
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Where(x => x.Length <= 100)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            throw new InvalidOperationException("User email is required.");
        }

        if (string.IsNullOrWhiteSpace(PasswordHash))
        {
            throw new InvalidOperationException("User password hash is required.");
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            throw new InvalidOperationException("User display name is required.");
        }

        ValidateProfile();
    }

    private void ValidateProfile()
    {
        if (DisplayName.Length > 100)
        {
            throw new InvalidOperationException("User display name is too long (max 100 characters).");
        }

        if (Bio is not null && Bio.Length > 1000)
        {
            throw new InvalidOperationException("User bio is too long (max 1000 characters).");
        }

        if (JobRole is not null && JobRole.Length > 200)
        {
            throw new InvalidOperationException("User job role is too long (max 200 characters).");
        }

        // Interests already normalized; additional checks would be redundant here.
    }
}
