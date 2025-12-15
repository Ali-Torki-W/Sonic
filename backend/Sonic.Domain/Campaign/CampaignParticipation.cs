using System;

namespace Sonic.Domain.Campaigns;

public sealed class CampaignParticipation
{
    public string Id { get; }
    public string PostId { get; }
    public string UserId { get; }
    public DateTime JoinedAt { get; }

    private CampaignParticipation(
        string id,
        string postId,
        string userId,
        DateTime joinedAt)
    {
        Id = id;
        PostId = postId;
        UserId = userId;
        JoinedAt = joinedAt;

        Validate();
    }

    public static CampaignParticipation CreateNew(string postId, string userId)
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

        return new CampaignParticipation(
            id: id,
            postId: postId.Trim(),
            userId: userId.Trim(),
            joinedAt: now);
    }

    public static CampaignParticipation FromPersistence(
        string id,
        string postId,
        string userId,
        DateTime joinedAt)
    {
        return new CampaignParticipation(
            id: id,
            postId: postId,
            userId: userId,
            joinedAt: joinedAt);
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
