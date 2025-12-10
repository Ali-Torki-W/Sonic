using Sonic.Domain.Posts;

namespace Sonic.Application.Posts.DTOs;

public sealed class PostResponse
{
    public string Id { get; set; } = default!;
    public PostType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();

    public string? ExternalLink { get; set; }

    public string AuthorId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsFeatured { get; set; }

    public long LikeCount { get; set; }

    public string? CampaignGoal { get; set; }

    public long ParticipantsCount { get; set; }
}