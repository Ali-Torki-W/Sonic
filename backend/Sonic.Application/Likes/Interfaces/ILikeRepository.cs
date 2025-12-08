using Sonic.Domain.Likes;

namespace Sonic.Application.Likes.interfaces;

public interface ILikeRepository
{
    Task<bool> ExistsAsync(
        string postId,
        string userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Like like,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        string postId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<long> CountForPostAsync(
        string postId,
        CancellationToken cancellationToken = default);
}
