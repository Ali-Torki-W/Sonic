using Sonic.Application.Common.Errors;
using Sonic.Application.Auth.interfaces;
using Sonic.Application.Users.DTOs;
using Sonic.Application.Users.interfaces;
using Sonic.Domain.Users;

namespace Sonic.Application.Users.Services;

public sealed class UserService(IUserRepository userRepository) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<GetCurrentUserResponse> GetCurrentUserAsync(
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw Errors.Unauthorized("User id claim is missing.", "auth.missing_sub");
        }

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken) ?? throw Errors.NotFound("User not found.", "user.not_found");
        return ToCurrentUserResponse(user);
    }

    public async Task<GetCurrentUserResponse> UpdateProfileAsync(
        string currentUserId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw Errors.Unauthorized("User id claim is missing.", "auth.missing_sub");
        }

        if (request is null)
        {
            throw Errors.BadRequest("Request body is required.", "request.required");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw Errors.BadRequest("DisplayName is required.", "user.display_name_required");
        }

        if (request.DisplayName.Length > 50)
        {
            throw Errors.BadRequest("DisplayName is too long (max 50).", "user.display_name_too_long");
        }

        if (request.Bio is not null && request.Bio.Length > 500)
        {
            throw Errors.BadRequest("Bio is too long (max 500).", "user.bio_too_long");
        }

        if (request.JobRole is not null && request.JobRole.Length > 80)
        {
            throw Errors.BadRequest("JobRole is too long (max 80).", "user.job_role_too_long");
        }

        if (request.AvatarUrl is not null &&
            request.AvatarUrl.Length > 300)
        {
            throw Errors.BadRequest("AvatarUrl is too long (max 300).", "user.avatar_url_too_long");
        }

        if (request.AvatarUrl is not null &&
            !Uri.TryCreate(request.AvatarUrl, UriKind.Absolute, out _))
        {
            throw Errors.BadRequest("AvatarUrl must be a valid absolute URL.", "user.avatar_url_invalid");
        }

        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken) ?? throw Errors.NotFound("User not found.", "user.not_found");

        // Domain should own rules; but you likely keep user mutable in service level.
        // Use a method on User if you have it; otherwise set and validate.
        user.UpdateProfile(
            displayName: request.DisplayName,
            bio: request.Bio,
            jobRole: request.JobRole,
            interests: request.Interests,
            avatarUrl: request.AvatarUrl);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return ToCurrentUserResponse(user);
    }

    public async Task<PublicProfileResponse> GetPublicProfileAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw Errors.BadRequest("User id is required.", "user.id_required");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken) ?? throw Errors.NotFound("User not found.", "user.not_found");
        return new PublicProfileResponse
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl
        };
    }

    private static GetCurrentUserResponse ToCurrentUserResponse(User user)
    {
        return new GetCurrentUserResponse
        {
            Id = user.Id,
            Email = user.Email,
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
