using Sonic.Application.Users.DTOs;

namespace Sonic.Application.Users.interfaces;

public interface IUserService
{
    Task<GetCurrentUserResponse> GetCurrentUserAsync(
        string currentUserId,
        CancellationToken cancellationToken = default);

    Task<GetCurrentUserResponse> UpdateProfileAsync(
        string currentUserId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);

    // Optional (MVP Lite)
    Task<PublicProfileResponse> GetPublicProfileAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
