namespace Sonic.Domain.Likes;

public sealed class Like
{
    public string Id { get; }
    public string PostId { get; }
    public string UserId { get; }
    public DateTime CreatedAt { get; }

    private Like(
        string id,
        string postId,
        string userId,
        DateTime createdAt)
    {
        Id = id;
        PostId = postId;
        UserId = userId;
        CreatedAt = createdAt;

        Validate();
    }

    public static Like CreateNew(string postId, string userId)
    {
        if (string.IsNullOrWhiteSpace(postId))
        {
            throw new ArgumentException("PostId is required.", nameof(postId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var now = DateTime.UtcNow;
        var id = Guid.NewGuid().ToString("N");

        return new Like(
            id: id,
            postId: postId.Trim(),
            userId: userId.Trim(),
            createdAt: now);
    }

    public static Like FromPersistence(
        string id,
        string postId,
        string userId,
        DateTime createdAt)
    {
        return new Like(
            id: id,
            postId: postId,
            userId: userId,
            createdAt: createdAt);
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(PostId))
        {
            throw new InvalidOperationException("PostId is required.");
        }

        if (string.IsNullOrWhiteSpace(UserId))
        {
            throw new InvalidOperationException("UserId is required.");
        }
    }
}
