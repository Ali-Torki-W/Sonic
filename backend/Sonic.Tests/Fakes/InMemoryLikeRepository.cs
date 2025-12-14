using Sonic.Application.Likes.interfaces;
using Sonic.Domain.Likes;

namespace Sonic.Tests.Fakes;

public sealed class InMemoryLikeRepository : ILikeRepository
{
    // Key: (PostId, UserId)
    private readonly HashSet<(string PostId, string UserId)> _likes = new();

    public Task<bool> ExistsAsync(string postId, string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(_likes.Contains((postId, userId)));

    public Task AddAsync(Like like, CancellationToken cancellationToken = default)
    {
        _likes.Add((like.PostId, like.UserId));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string postId, string userId, CancellationToken cancellationToken = default)
    {
        _likes.Remove((postId, userId));
        return Task.CompletedTask;
    }

    public Task<long> CountForPostAsync(string postId, CancellationToken cancellationToken = default)
        => Task.FromResult(_likes.LongCount(x => x.PostId == postId));
}
