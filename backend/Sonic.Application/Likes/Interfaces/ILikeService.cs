using Sonic.Application.Likes.DTOs;

namespace Sonic.Application.Likes.interfaces;

public interface ILikeService
{
    Task<LikeToggleResponse> ToggleLikeAsync(
        string postId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<LikeToggleResponse> GetStatusAsync(
        string postId,
        string userId,
        CancellationToken cancellationToken = default);
}
